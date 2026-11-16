namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Auth;
using Atlas.Application.Contracts;
using Atlas.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithValidation<LoginRequest>()
            .WithName("Login")
            .WithSummary("Sign in")
            .WithDescription("Checks user name and password and sets the HttpOnly login cookie (30 days, sliding). "
                + "After 5 wrong passwords the user is locked out for 5 minutes.")
            .Produces<MeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Anonymous so it always answers 204, even when the cookie has already expired.
        group.MapPost("/logout", Logout)
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Sign out")
            .WithDescription("Deletes the login cookie.");

        group.MapGet("/me", GetMe)
            .WithName("GetMe")
            .WithSummary("The signed-in user")
            .WithDescription("Returns 401 when nobody is signed in.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
        => (await authService.LoginAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<NoContent> Logout(IAuthService authService)
    {
        await authService.LogoutAsync();
        return TypedResults.NoContent();
    }

    // Only reached with a valid cookie (fallback policy), so the claims are there.
    private static Results<Ok<MeResponse>, UnauthorizedHttpResult> GetMe(ICurrentUser currentUser)
        => currentUser is { UserId: { } id, UserName: { } userName, Role: { } role }
            ? TypedResults.Ok(new MeResponse(id, userName, role, currentUser.CompanyId))
            : TypedResults.Unauthorized();
}
