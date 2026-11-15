using System.Net;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class SmokeTests(AtlasApiFactory factory)
{
    [Fact]
    public async Task OpenApiDocument_IsServed_InDevelopment()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
