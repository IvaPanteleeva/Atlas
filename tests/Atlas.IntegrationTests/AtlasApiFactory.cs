using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Atlas.IntegrationTests;

// One throw-away Postgres container for all integration tests (shared through the "Api" collection).
// Migrations run on startup against it, like in Development.
public sealed class AtlasApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public const string TestOwnerUserName = "test-owner";

    public const string TestOwnerPassword = "test-password-123";

    private Respawner? _respawner;

    public async Task InitializeAsync() => await _database.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Atlas", _database.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Seed:Owner:UserName", TestOwnerUserName);
        builder.UseSetting("Seed:Owner:Password", TestOwnerPassword);
    }

    // Deletes all data between tests, keeps the schema, the migrations history and the seed data
    // (roles, companies). Users are deleted too, including the Owner seeded at startup.
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();

        if (_respawner is null)
        {
            try
            {
                _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = ["public"],
                    TablesToIgnore =
                    [
                        new Table("__EFMigrationsHistory"),
                        new Table("AspNetRoles"),
                        new Table("Companies"),
                    ],
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("No tables found", StringComparison.Ordinal))
            {
                // No entities/migrations yet, so there is nothing to clean. Respawn throws in this case;
                // the respawner is not cached, so it is created once the first tables exist.
                return;
            }
        }

        await _respawner.ResetAsync(connection);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<AtlasApiFactory>;
