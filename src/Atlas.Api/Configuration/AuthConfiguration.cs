namespace Atlas.Api.Configuration;

using Atlas.Api.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

// Cookie sign-in and policies (CLAUDE.md "Вход и роли"). Users, roles and SignInManager are in AddInfrastructure.
public static class AuthConfiguration
{
    public const string CookieName = "atlas.auth";

    public static IServiceCollection AddAtlasAuth(this IServiceCollection services)
    {
        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;

            // An API answers with a status code instead of redirecting to a login page.
            // The bodies stay empty here; UseStatusCodePages turns them into ProblemDetails.
            // The existing Events object is kept: Identity puts its security stamp check there.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(Policies.Owner, policy => policy.RequireRole(Roles.Owner))
            .AddPolicy(Policies.Staff, policy => policy.RequireRole(Roles.Owner, Roles.Crew))
            .AddPolicy(Policies.Client, policy => policy.RequireRole(Roles.ClientViewer))
            // Every endpoint needs a signed-in user unless it says AllowAnonymous (login, health, ...).
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
