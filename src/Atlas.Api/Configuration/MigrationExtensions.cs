namespace Atlas.Api.Configuration;

using Atlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class MigrationExtensions
{
    // Development only, behind Database:MigrateOnStartup so tests can switch it off.
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        await db.Database.MigrateAsync();
    }
}
