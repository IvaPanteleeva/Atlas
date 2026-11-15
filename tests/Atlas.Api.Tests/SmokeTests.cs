using System.Net;

namespace Atlas.Api.Tests;

public class SmokeTests(AtlasApiFactory factory) : IClassFixture<AtlasApiFactory>
{
    [Fact]
    public async Task OpenApiDocument_IsServed_InDevelopment()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
