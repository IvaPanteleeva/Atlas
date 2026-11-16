using Atlas.Application.Identity;
using Atlas.Infrastructure.Entities;
using Atlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.IntegrationTests;

// Checks what the startup seed leaves in the database. Read-only, so no ResetDatabaseAsync.
[Collection("Api")]
public class IdentitySeedTests(AtlasApiFactory factory)
{
    [Fact]
    public async Task AfterStartup_ExactlyTheThreeRolesExist()
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var roleNames = await roleManager.Roles.Select(r => r.Name).ToListAsync();

        Assert.Equal(
            [Roles.ClientViewer, Roles.Manager, Roles.Owner],
            roleNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task AfterStartup_ZhenistroyCompanyExists()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();

        var company = await db.Companies.SingleOrDefaultAsync(c => c.Name == "ЖЕНИСТРОЙ ЕООД");

        Assert.NotNull(company);
        Assert.Equal(CompanyConfiguration.ZhenistroyId, company.Id);
    }

    [Fact]
    public async Task AfterStartup_ConfiguredOwnerExists_WithOwnerRoleAndPassword()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AtlasUser>>();

        var owner = await userManager.FindByNameAsync(AtlasApiFactory.TestOwnerUserName);

        Assert.NotNull(owner);
        Assert.True(await userManager.IsInRoleAsync(owner, Roles.Owner));
        Assert.True(await userManager.CheckPasswordAsync(owner, AtlasApiFactory.TestOwnerPassword));
    }
}
