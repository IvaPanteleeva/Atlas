using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Api.Tests;

public class ApiRoutingTests(AtlasApiFactory factory) : IClassFixture<AtlasApiFactory>
{
    [Theory]
    [InlineData("/api/does-not-exist")]
    [InlineData("/api/sites/5/does-not-exist")]
    public async Task UnknownApiRoute_Returns404ProblemDetails_NotHtml(string url)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem?.Status);
        Assert.Equal("Няма такъв адрес в API-то", problem?.Title);
    }
}
