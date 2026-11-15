using System.Net;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class HealthEndpointTests(AtlasApiFactory factory)
{
    [Fact]
    public async Task Health_WithRealDatabase_Returns200AndOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"status":"ok","database":"ok"}""", await response.Content.ReadAsStringAsync());
    }
}
