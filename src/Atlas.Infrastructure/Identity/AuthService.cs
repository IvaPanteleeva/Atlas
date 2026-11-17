namespace Atlas.Infrastructure.Identity;

using Atlas.Application.Auth;
using Atlas.Application.Common;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;

public sealed class AuthService(
    SignInManager<AtlasUser> signInManager,
    UserManager<AtlasUser> userManager) : IAuthService
{
    // Same message for an unknown user and a wrong password, so the answer does not reveal which names exist.
    public const string WrongCredentialsMessage = "Грешно име или парола";

    public const string LockedOutMessage = "Твърде много опити. Опитай пак след 5 минути.";

    // No CancellationToken in SignInManager/UserManager; checked once before the work starts.
    public async Task<Result<MeResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // isPersistent: the cookie survives closing the browser (30 days, sliding).
        // lockoutOnFailure: 5 wrong passwords lock the user for 5 minutes (IdentityOptionsSetup).
        var signIn = await signInManager.PasswordSignInAsync(
            request.UserName,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            return Error.Unauthorized(LockedOutMessage);
        }

        if (!signIn.Succeeded)
        {
            return Error.Unauthorized(WrongCredentialsMessage);
        }

        var user = await userManager.FindByNameAsync(request.UserName)
            ?? throw new InvalidOperationException($"User {request.UserName} signed in but was not found.");
        var roles = await userManager.GetRolesAsync(user);

        // One role per user (decided in 2.2).
        return new MeResponse(user.Id, user.UserName!, roles.Single(), user.CompanyId, user.PersonId);
    }

    public async Task LogoutAsync()
        => await signInManager.SignOutAsync();
}
