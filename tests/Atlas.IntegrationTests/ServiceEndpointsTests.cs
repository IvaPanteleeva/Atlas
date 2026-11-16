using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Identity;
using Atlas.Application.Services;
using Atlas.Application.SitePrices;
using Atlas.Application.Sites;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class ServiceEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "crew-password";

    private const string NotFoundTitle = "Няма такава услуга.";

    private const string DuplicateTitle = "Тази услуга вече я има";

    // Same as the API: enums travel as strings ("SquareMeter").
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var id = Guid.NewGuid();

        var list = await client.GetAsync("/api/services");
        var create = await client.PostAsJsonAsync("/api/services", new CreateServiceRequest("Шпакловка", Unit.SquareMeter), Json);
        var update = await client.PutAsJsonAsync($"/api/services/{id}", new UpdateServiceRequest("Шпакловка", Unit.SquareMeter), Json);
        var delete = await client.DeleteAsync($"/api/services/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync("/api/services");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("application/problem+json", list.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crew_CanList_ButNotWrite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter, 6.5m));
        await factory.CreateUserAsync("crew", Password, Roles.Crew);
        var crew = await factory.CreateLoggedInClientAsync("crew", Password);

        var list = await crew.GetAsync("/api/services");
        var create = await crew.PostAsJsonAsync("/api/services", new CreateServiceRequest("Боя", Unit.SquareMeter), Json);
        var update = await crew.PutAsJsonAsync(
            $"/api/services/{service.Id}",
            new UpdateServiceRequest("Боя", Unit.SquareMeter),
            Json);
        var delete = await crew.DeleteAsync($"/api/services/{service.Id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal([service], (await list.Content.ReadFromJsonAsync<ServiceResponse[]>(Json))!);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_AndTrimmedName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/services",
            new CreateServiceRequest(" Шпакловка ", Unit.SquareMeter, 6.5m),
            Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var service = await response.Content.ReadFromJsonAsync<ServiceResponse>(Json);
        Assert.NotNull(service);
        Assert.Equal($"/api/services/{service.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(new ServiceResponse(service.Id, "Шпакловка", Unit.SquareMeter, 6.5m), service);

        var services = await owner.GetFromJsonAsync<ServiceResponse[]>("/api/services", Json);
        Assert.Equal([service], services!);
    }

    [Fact]
    public async Task Json_HasUnitAsString_AndDefaultRateAsNumber()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await owner.PostAsJsonAsync("/api/services", new { name = "Шпакловка", unit = "SquareMeter", defaultRate = 6.5 });

        var body = JsonNode.Parse(await owner.GetStringAsync("/api/services"))!.AsArray();

        var service = Assert.Single(body)!;
        Assert.Equal("SquareMeter", service["unit"]!.GetValue<string>());
        Assert.Equal(JsonValueKind.Number, service["defaultRate"]!.GetValueKind());
        Assert.Equal(6.5m, service["defaultRate"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Create_WithoutDefaultRate_IsOk_AndReturnsNull()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/services", new { name = "Демонтаж", unit = "Day" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Null(body["defaultRate"]);
        Assert.Equal("Day", body["unit"]!.GetValue<string>());
    }

    [Fact]
    public async Task List_IsOrderedByName_ThenUnit()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.Piece));
        await CreateAsync(owner, new CreateServiceRequest("Боя", Unit.SquareMeter));
        await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.LinearMeter));
        await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));

        var services = await owner.GetFromJsonAsync<ServiceResponse[]>("/api/services", Json);

        Assert.Equal(
            [("Боя", Unit.SquareMeter), ("Шпакловка", Unit.LinearMeter), ("Шпакловка", Unit.SquareMeter), ("Шпакловка", Unit.Piece)],
            services!.Select(s => (s.Name, s.Unit)));
    }

    [Theory]
    [InlineData("Шпакловка")]
    [InlineData("  Шпакловка ")]
    [InlineData("ШПАКЛОВКА")]
    [InlineData("шпакловка")]
    public async Task Create_SameNameAndUnit_Returns409(string name)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));

        var response = await owner.PostAsJsonAsync("/api/services", new CreateServiceRequest(name, Unit.SquareMeter), Json);

        await AssertConflict(response, DuplicateTitle);
    }

    [Fact]
    public async Task Create_SameNameOtherUnit_Returns201()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));

        var response = await owner.PostAsJsonAsync(
            "/api/services",
            new CreateServiceRequest("шпакловка", Unit.LinearMeter),
            Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingUnit_Returns400OnUnit()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/services", new { name = "Шпакловка" });

        await AssertFieldError(response, "unit");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    public async Task Create_EmptyName_Returns400OnName(string name)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/services", new CreateServiceRequest(name, Unit.Day), Json);

        await AssertFieldError(response, "name");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("6.555")]
    [InlineData("100000.01")]
    public async Task Create_BadDefaultRate_Returns400OnDefaultRate(string rate)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsync(
            "/api/services",
            JsonBody($$"""{ "name": "Шпакловка", "unit": "SquareMeter", "defaultRate": {{rate}} }"""));

        await AssertFieldError(response, "defaultRate");
    }

    [Fact]
    public async Task Create_DefaultRate100000_IsOk()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var service = await CreateAsync(owner, new CreateServiceRequest("Покрив", Unit.Piece, 100_000m));

        Assert.Equal(100_000m, service.DefaultRate);
    }

    [Fact]
    public async Task Owner_Updates_Returns200_AndReplacesAllFields()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter, 6.5m));

        var response = await owner.PutAsJsonAsync(
            $"/api/services/{service.Id}",
            new UpdateServiceRequest(" Шпакловка таван ", Unit.LinearMeter, null),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expected = new ServiceResponse(service.Id, "Шпакловка таван", Unit.LinearMeter, null);
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<ServiceResponse>(Json));
        Assert.Equal([expected], (await owner.GetFromJsonAsync<ServiceResponse[]>("/api/services", Json))!);
    }

    [Fact]
    public async Task Update_KeepingItsOwnNameAndUnit_Returns200()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter, 6.5m));

        var response = await owner.PutAsJsonAsync(
            $"/api/services/{service.Id}",
            new UpdateServiceRequest("ШПАКЛОВКА", Unit.SquareMeter, 7m),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ToAnotherServicesNameAndUnit_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));
        var other = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.LinearMeter));

        var response = await owner.PutAsJsonAsync(
            $"/api/services/{other.Id}",
            new UpdateServiceRequest(" шпакловка", Unit.SquareMeter),
            Json);

        await AssertConflict(response, DuplicateTitle);
    }

    [Fact]
    public async Task Update_MissingUnit_Returns400OnUnit()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));

        var response = await owner.PutAsJsonAsync($"/api/services/{service.Id}", new { name = "Шпакловка" });

        await AssertFieldError(response, "unit");
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PutAsJsonAsync(
            $"/api/services/{Guid.NewGuid()}",
            new UpdateServiceRequest("Шпакловка", Unit.SquareMeter),
            Json);

        await AssertNotFound(response);
    }

    [Fact]
    public async Task Owner_DeletesUnusedService_Returns204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));

        var response = await owner.DeleteAsync($"/api/services/{service.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<ServiceResponse[]>("/api/services", Json))!);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.DeleteAsync($"/api/services/{Guid.NewGuid()}");

        await AssertNotFound(response);
    }

    [Fact]
    public async Task Delete_ServiceWithPriceOnOneSite_Returns409_InOneSite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));
        await AddPriceAsync(owner, await CreateSiteAsync(owner, "Баня"), service.Id, 7m);

        var response = await owner.DeleteAsync($"/api/services/{service.Id}");

        await AssertConflict(response, "Услугата има цени в 1 обект");
        Assert.Single((await owner.GetFromJsonAsync<ServiceResponse[]>("/api/services", Json))!);
    }

    [Fact]
    public async Task Delete_ServiceWithPricesOnTwoSites_Returns409_InTwoSites()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateAsync(owner, new CreateServiceRequest("Шпакловка", Unit.SquareMeter));
        await AddPriceAsync(owner, await CreateSiteAsync(owner, "Баня"), service.Id, 7m);
        await AddPriceAsync(owner, await CreateSiteAsync(owner, "Кухня"), service.Id, 8m);

        var response = await owner.DeleteAsync($"/api/services/{service.Id}");

        await AssertConflict(response, "Услугата има цени в 2 обекта");
    }

    private static StringContent JsonBody(string json)
        => new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task<ServiceResponse> CreateAsync(HttpClient owner, CreateServiceRequest request)
    {
        var response = await owner.PostAsJsonAsync("/api/services", request, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var service = await response.Content.ReadFromJsonAsync<ServiceResponse>(Json);
        Assert.NotNull(service);
        return service;
    }

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task AddPriceAsync(HttpClient owner, Guid siteId, Guid serviceId, decimal rate)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(serviceId, rate),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertConflict(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(NotFoundTitle, problem?.Title);
    }

    private static async Task AssertFieldError(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal([field], problem.Errors.Keys);
    }
}
