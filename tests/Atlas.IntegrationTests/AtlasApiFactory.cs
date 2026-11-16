using System.Net.Http.Json;
using Atlas.Infrastructure.Entities;
using Atlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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
    // (roles, companies). Users are deleted too, so the startup seed runs again to bring back the test Owner.
    public async Task ResetDatabaseAsync()
    {
        // Starts the app if no test has done it yet: the migrations run on startup, Respawn needs the tables.
        _ = Services;

        await using (var connection = new NpgsqlConnection(_database.GetConnectionString()))
        {
            await connection.OpenAsync();

            _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
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

            await _respawner.ResetAsync(connection);

            // Companies is ignored above to keep the seed; companies added by tests go,
            // and the seed gets its name back in case a test renamed it.
            await using var companies = new NpgsqlCommand(
                """
                DELETE FROM "Companies" WHERE "Id" <> @zhenistroy;
                UPDATE "Companies" SET "Name" = @zhenistroyName WHERE "Id" = @zhenistroy;
                """,
                connection);
            companies.Parameters.AddWithValue("zhenistroy", CompanyConfiguration.ZhenistroyId);
            companies.Parameters.AddWithValue("zhenistroyName", CompanyConfiguration.ZhenistroyName);
            await companies.ExecuteNonQueryAsync();
        }

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
    }

    // Creates a user directly through UserManager (no HTTP), e.g. a Crew member for a 403 test.
    public async Task<AtlasUser> CreateUserAsync(string userName, string password, string role, Guid? companyId = null)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AtlasUser>>();

        var user = new AtlasUser { UserName = userName, CompanyId = companyId };
        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
        var inRole = await userManager.AddToRoleAsync(user, role);
        Assert.True(inRole.Succeeded, string.Join(", ", inRole.Errors.Select(e => e.Description)));

        return user;
    }

    // A client whose cookie container holds the login cookie of the given user.
    public async Task<HttpClient> CreateLoggedInClientAsync(string userName, string password)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        Assert.True(response.IsSuccessStatusCode, $"Login as {userName} failed with {(int)response.StatusCode}.");

        return client;
    }

    public Task<HttpClient> CreateOwnerClientAsync()
        => CreateLoggedInClientAsync(TestOwnerUserName, TestOwnerPassword);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<AtlasApiFactory>;
