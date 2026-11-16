namespace Atlas.Infrastructure.Configuration;

using Microsoft.AspNetCore.Identity;

// Password and lockout rules from CLAUDE.md "Вход и роли". Public so unit tests use the same rules.
public static class IdentityOptionsSetup
{
    public static void Apply(IdentityOptions options)
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;

        // Login is by user name; e-mail is optional.
        options.User.RequireUniqueEmail = false;
    }
}
