using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Identity;
using Atlas.Application.Services;
using Atlas.Application.SitePrices;
using Atlas.Application.Sites;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class SitePriceEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "crew-password";

    private const string SiteNotFoundTitle = "Няма такъв обект.";

    private const string PriceNotFoundTitle = "Няма такава цена.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var siteId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/sites/{siteId}/prices");
        var create = await client.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(Guid.NewGuid(), 5m),
            Json);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_AndServiceData()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter, 6.5m);

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(service.Id, 7.25m),
            Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var price = await response.Content.ReadFromJsonAsync<SitePriceResponse>(Json);
        Assert.NotNull(price);
        Assert.Equal($"/api/sites/{siteId}/prices/{price.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(new SitePriceResponse(price.Id, service.Id, "Шпакловка", Unit.SquareMeter, 7.25m, 6.5m), price);

        var prices = await owner.GetFromJsonAsync<SitePriceResponse[]>($"/api/sites/{siteId}/prices", Json);
        Assert.Equal([price], prices!);
    }

    [Fact]
    public async Task List_IsOrderedByServiceName_AndOnlyForThatSite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var plaster = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        var paint = await CreateServiceAsync(owner, "Боя", Unit.SquareMeter, 4m);
        var demolition = await CreateServiceAsync(owner, "Демонтаж", Unit.Day);
        await AddPriceAsync(owner, bath, plaster.Id, 7m);
        await AddPriceAsync(owner, bath, paint.Id, 5m);
        await AddPriceAsync(owner, kitchen, demolition.Id, 120m);

        var prices = await owner.GetFromJsonAsync<SitePriceResponse[]>($"/api/sites/{bath}/prices", Json);

        Assert.Equal(["Боя", "Шпакловка"], prices!.Select(p => p.ServiceName));
        Assert.Equal([5m, 7m], prices!.Select(p => p.Rate));
        Assert.Equal([4m, null], prices!.Select(p => p.DefaultRate));
    }

    [Fact]
    public async Task List_SiteWithoutPrices_IsEmpty()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var prices = await owner.GetFromJsonAsync<SitePriceResponse[]>($"/api/sites/{siteId}/prices", Json);

        Assert.Empty(prices!);
    }

    [Fact]
    public async Task UnknownSite_Returns404_OnGetAndPost()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        var siteId = Guid.NewGuid();

        var list = await owner.GetAsync($"/api/sites/{siteId}/prices");
        var create = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(service.Id, 7m),
            Json);

        await AssertNotFound(list, SiteNotFoundTitle);
        await AssertNotFound(create, SiteNotFoundTitle);
    }

    [Fact]
    public async Task Create_UnknownService_Returns400OnServiceId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(Guid.NewGuid(), 7m),
            Json);

        var problem = await AssertFieldError(response, "serviceId");
        Assert.Equal(["Няма такава услуга."], problem.Errors["serviceId"]);
    }

    [Fact]
    public async Task Create_MissingServiceId_Returns400OnServiceId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync($"/api/sites/{siteId}/prices", new { rate = 7 });

        await AssertFieldError(response, "serviceId");
    }

    [Fact]
    public async Task Create_SameServiceTwiceOnSite_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        await AddPriceAsync(owner, siteId, service.Id, 7m);

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(service.Id, 8m),
            Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Тази услуга вече има цена за обекта", problem?.Title);
    }

    [Fact]
    public async Task Create_SameServiceOnTwoSites_IsOk()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        await AddPriceAsync(owner, await CreateSiteAsync(owner, "Баня"), service.Id, 7m);

        await AddPriceAsync(owner, await CreateSiteAsync(owner, "Кухня"), service.Id, 8m);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("7.125")]
    [InlineData("100000.5")]
    [InlineData("null")]
    public async Task Create_BadRate_Returns400OnRate(string rate)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);

        var response = await owner.PostAsync(
            $"/api/sites/{siteId}/prices",
            JsonBody($$"""{ "serviceId": "{{service.Id}}", "rate": {{rate}} }"""));

        await AssertFieldError(response, "rate");
    }

    [Fact]
    public async Task Owner_Updates_Returns200()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter, 6.5m);
        var price = await AddPriceAsync(owner, siteId, service.Id, 7m);

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/prices/{price.Id}",
            new UpdateSitePriceRequest(7.5m),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expected = price with { Rate = 7.5m };
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<SitePriceResponse>(Json));
        Assert.Equal([expected], (await owner.GetFromJsonAsync<SitePriceResponse[]>($"/api/sites/{siteId}/prices", Json))!);
    }

    [Fact]
    public async Task Update_MissingRate_Returns400OnRate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        var price = await AddPriceAsync(owner, siteId, service.Id, 7m);

        var response = await owner.PutAsJsonAsync($"/api/sites/{siteId}/prices/{price.Id}", new { });

        await AssertFieldError(response, "rate");
    }

    [Fact]
    public async Task Owner_Deletes_Returns204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        var price = await AddPriceAsync(owner, siteId, service.Id, 7m);

        var response = await owner.DeleteAsync($"/api/sites/{siteId}/prices/{price.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<SitePriceResponse[]>($"/api/sites/{siteId}/prices", Json))!);
    }

    [Fact]
    public async Task UnknownPrice_Returns404_OnPutAndDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var priceId = Guid.NewGuid();

        var update = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/prices/{priceId}",
            new UpdateSitePriceRequest(7m),
            Json);
        var delete = await owner.DeleteAsync($"/api/sites/{siteId}/prices/{priceId}");

        await AssertNotFound(update, PriceNotFoundTitle);
        await AssertNotFound(delete, PriceNotFoundTitle);
    }

    [Fact]
    public async Task PriceOfAnotherSite_Returns404_OnPutAndDelete_AndStaysUnchanged()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        var bathPrice = await AddPriceAsync(owner, bath, service.Id, 7m);

        var update = await owner.PutAsJsonAsync(
            $"/api/sites/{kitchen}/prices/{bathPrice.Id}",
            new UpdateSitePriceRequest(99m),
            Json);
        var delete = await owner.DeleteAsync($"/api/sites/{kitchen}/prices/{bathPrice.Id}");

        await AssertNotFound(update, PriceNotFoundTitle);
        await AssertNotFound(delete, PriceNotFoundTitle);
        Assert.Equal([bathPrice], (await owner.GetFromJsonAsync<SitePriceResponse[]>($"/api/sites/{bath}/prices", Json))!);
    }

    [Fact]
    public async Task Crew_CanList_ButNotWrite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        var price = await AddPriceAsync(owner, siteId, service.Id, 7m);
        await factory.CreateUserAsync("crew", Password, Roles.Crew);
        var crew = await factory.CreateLoggedInClientAsync("crew", Password);

        var list = await crew.GetAsync($"/api/sites/{siteId}/prices");
        var create = await crew.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(service.Id, 8m),
            Json);
        var update = await crew.PutAsJsonAsync(
            $"/api/sites/{siteId}/prices/{price.Id}",
            new UpdateSitePriceRequest(8m),
            Json);
        var delete = await crew.DeleteAsync($"/api/sites/{siteId}/prices/{price.Id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal([price], (await list.Content.ReadFromJsonAsync<SitePriceResponse[]>(Json))!);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await factory.CreateUserAsync(
            "zhenistroy",
            Password,
            Roles.ClientViewer,
            Atlas.Infrastructure.Entities.CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync($"/api/sites/{siteId}/prices");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    // Prices are not history: they go with the site, and then the service is free to delete.
    [Fact]
    public async Task DeletingTheSite_RemovesItsPrices_ThenServiceCanBeDeleted()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var service = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter);
        await AddPriceAsync(owner, siteId, service.Id, 7m);

        var deleteSite = await owner.DeleteAsync($"/api/sites/{siteId}");
        var list = await owner.GetAsync($"/api/sites/{siteId}/prices");
        var deleteService = await owner.DeleteAsync($"/api/services/{service.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteSite.StatusCode);
        await AssertNotFound(list, SiteNotFoundTitle);
        Assert.Equal(HttpStatusCode.NoContent, deleteService.StatusCode);
    }

    private static StringContent JsonBody(string json)
        => new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task<ServiceResponse> CreateServiceAsync(
        HttpClient owner,
        string name,
        Unit unit,
        decimal? defaultRate = null)
    {
        var response = await owner.PostAsJsonAsync("/api/services", new CreateServiceRequest(name, unit, defaultRate), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var service = await response.Content.ReadFromJsonAsync<ServiceResponse>(Json);
        Assert.NotNull(service);
        return service;
    }

    private static async Task<SitePriceResponse> AddPriceAsync(HttpClient owner, Guid siteId, Guid serviceId, decimal rate)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(serviceId, rate),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var price = await response.Content.ReadFromJsonAsync<SitePriceResponse>(Json);
        Assert.NotNull(price);
        return price;
    }

    private static async Task AssertNotFound(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
    }

    private static async Task<ValidationProblemDetails> AssertFieldError(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal([field], problem.Errors.Keys);
        return problem;
    }
}
