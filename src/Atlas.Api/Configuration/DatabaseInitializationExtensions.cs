namespace Atlas.Api.Configuration;

using Atlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class DatabaseInitializationExtensions
{
    // 1. Migrations: Development only, behind Database:MigrateOnStartup so tests can switch it off.
    // 2. Roles and the Owner: every environment; safe to run on every start.
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
            await db.Database.MigrateAsync();
        }

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }
}
