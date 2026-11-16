namespace Atlas.Infrastructure.Persistence;

using Atlas.Application.Identity;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Runs on every start, in every environment. Creates only what is missing, so it never duplicates.
// The Owner comes from configuration: appsettings.Development.json locally (throwaway values),
// environment variables in production (Heroku Config Vars Seed__Owner__UserName / Seed__Owner__Password).
// Empty values in appsettings.json count as missing; that is only warned about while no Owner exists.
public sealed class IdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<AtlasUser> userManager,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger)
{
    // No CancellationToken: RoleManager/UserManager methods do not take one.
    public async Task SeedAsync()
    {
        await this.SeedRolesAsync();
        await this.SeedOwnerAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)), $"role {role}");
            }
        }
    }

    private async Task SeedOwnerAsync()
    {
        var userName = configuration["Seed:Owner:UserName"];
        var password = configuration["Seed:Owner:Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            // Without config it is a problem only if nobody can sign in as Owner (e.g. a new database).
            // Once an Owner exists (created earlier or through POST /api/users), the config may be removed.
            if ((await userManager.GetUsersInRoleAsync(Roles.Owner)).Count == 0)
            {
                logger.LogWarning(
                    "Owner не е конфигуриран: липсват Seed:Owner:UserName и/или Seed:Owner:Password, а потребител с роля Owner няма. Owner не е създаден.");
            }

            return;
        }

        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var owner = new AtlasUser { UserName = userName };
        EnsureSucceeded(await userManager.CreateAsync(owner, password), $"Owner {userName}");
        EnsureSucceeded(await userManager.AddToRoleAsync(owner, Roles.Owner), $"Owner role for {userName}");

        logger.LogInformation("Owner {UserName} е създаден.", userName);
    }

    // A bad seed config (e.g. a too short password) stops the start instead of leaving the app without an Owner.
    private static void EnsureSucceeded(IdentityResult result, string what)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Seeding {what} failed: {errors}");
        }
    }
}
