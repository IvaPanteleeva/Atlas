using Atlas.Application.Identity;
using Atlas.Infrastructure.Entities;
using Atlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

// Round 28: the Crew role became Manager. The migration RenameCrewToManager moves existing Crew users to Manager.
// These tests step the shared database one migration back and forward again; the finally blocks always bring it
// back to the latest migration.
[Collection("Api")]
public class RenameCrewToManagerMigrationTests(AtlasApiFactory factory)
{
    private const string MigrationBefore = "AddSiteContacts";

    [Fact]
    public async Task Down_TurnsManagersIntoCrew_Up_TurnsThemBack_LeavingExactlyTheThreeRoles()
    {
        await factory.ResetDatabaseAsync();
        var user = await factory.CreateUserAsync("manager", "user-password", Roles.Manager);

        try
        {
            await this.MigrateAsync(MigrationBefore);
            Assert.Equal(["ClientViewer", "Crew", "Owner"], await this.RoleNamesAsync());
            Assert.Equal(["Crew"], await this.RolesOfAsync(user.Id));

            await this.MigrateAsync(null);
            Assert.Equal([Roles.ClientViewer, Roles.Manager, Roles.Owner], await this.RoleNamesAsync());
            Assert.Equal([Roles.Manager], await this.RolesOfAsync(user.Id));
        }
        finally
        {
            await this.MigrateAsync(null);
        }
    }

    // In production the migrations are not applied on start, so the seed may already have created Manager
    // while Crew still has users. Up then moves those users to Manager and drops Crew.
    [Fact]
    public async Task Up_WhenTheSeedAlreadyCreatedManager_MovesCrewUsersAndDropsCrew()
    {
        await factory.ResetDatabaseAsync();
        var user = await factory.CreateUserAsync("manager", "user-password", Roles.Manager);

        try
        {
            await this.MigrateAsync(MigrationBefore);
            using (var scope = factory.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Manager))).Succeeded);
            }

            await this.MigrateAsync(null);

            Assert.Equal([Roles.ClientViewer, Roles.Manager, Roles.Owner], await this.RoleNamesAsync());
            Assert.Equal([Roles.Manager], await this.RolesOfAsync(user.Id));
        }
        finally
        {
            await this.MigrateAsync(null);
        }
    }

    // null = the latest migration.
    private async Task MigrateAsync(string? target)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        await db.GetService<IMigrator>().MigrateAsync(target);
    }

    private async Task<string[]> RoleNamesAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        var names = await db.Roles.AsNoTracking().Select(r => r.Name!).ToListAsync();
        Assert.Equal(
            names.Select(n => n.ToUpperInvariant()).Order(StringComparer.Ordinal),
            await db.Roles.AsNoTracking().Select(r => r.NormalizedName!).OrderBy(n => n).ToListAsync());
        return [.. names.Order(StringComparer.Ordinal)];
    }

    private async Task<string[]> RolesOfAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        return await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                where userRole.UserId == userId
                orderby role.Name
                select role.Name!)
            .ToArrayAsync();
    }
}
