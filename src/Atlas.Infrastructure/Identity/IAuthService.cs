namespace Atlas.Infrastructure.Identity;

using Atlas.Application.Auth;
using Atlas.Application.Common;

// Sign-in and sign-out with the Identity cookie.
public interface IAuthService
{
    // Unauthorized when the name/password is wrong or the user is locked out; the cookie is set on success.
    Task<Result<MeResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task LogoutAsync();
}
