using System.Net;

namespace Atlas.IntegrationTests;

// Every /api/... route needs a login by default (fallback policy); these stay open.
[Collection("Api")]
public class AnonymousAccessTests(AtlasApiFactory factory)
{
    [Fact]
    public async Task Health_IsOpen()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownApiRoute_Is404_Not401()
    {
        var response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsOpen()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScalarUi_IsOpen()
    {
        var response = await factory.CreateClient().GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Angular route: the server answers with index.html, Angular decides about the login itself.
    // In tests wwwroot may have no index.html, so the answer can be 404, but never 401.
    [Fact]
    public async Task AngularRoute_IsNot401()
    {
        var response = await factory.CreateClient().GetAsync("/sites");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
