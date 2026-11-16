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
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class WorkDayEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private const string SiteNotFoundTitle = "Няма такъв обект.";

    private const string DayNotFoundTitle = "Няма такъв работен ден.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Same rule as the server: the date in Bulgaria.
    private static DateOnly Today => WorkDayRules.Today(TimeProvider.System);

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var siteId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/sites/{siteId}/workdays");
        var create = await client.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day(Today, Free("Плочки")), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_ItemsInOrder_AndSums()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var date = Today.AddDays(-1);

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            new SaveWorkDayRequest(
                date,
                "  Първи ден  ",
                "   ",
                [
                    Free("  Плочки  ", 2m, Unit.SquareMeter, 10m),
                    Free("Демонтаж", 1m, Unit.Day, null),
                    Free("Фуги", 1m, Unit.Piece, 5m, needsReview: true),
                ]),
            Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json);
        Assert.NotNull(day);
        Assert.Equal($"/api/sites/{siteId}/workdays/{day.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(siteId, day.SiteId);
        Assert.Equal(date, day.Date);
        Assert.Equal("Първи ден", day.Title);
        Assert.Null(day.Note);
        Assert.Equal(["Плочки", "Демонтаж", "Фуги"], day.Items.Select(i => i.Description));
        Assert.Equal([20m, null, 5m], day.Items.Select(i => i.Amount));
        Assert.Equal([10m, null, 5m], day.Items.Select(i => i.Rate));
        Assert.Equal([Unit.SquareMeter, Unit.Day, Unit.Piece], day.Items.Select(i => i.Unit));
        Assert.Equal([false, false, true], day.Items.Select(i => i.NeedsReview));
        Assert.All(day.Items, i => Assert.NotEqual(Guid.Empty, i.Id));

        // The flagged item with a price counts in the total; the open count has the unpriced and the flagged one.
        Assert.Equal(25m, day.Total);
        Assert.Equal(2, day.OpenCount);

        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        AssertSameDay(day, read!);
    }

    [Fact]
    public async Task Date_TravelsAsIsoDate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsync(
            $"/api/sites/{siteId}/workdays",
            JsonBody("""
                {
                  "date": "2026-03-05",
                  "items": [{ "description": "Плочки", "quantity": 1.5, "unit": "SquareMeter", "rate": 12, "needsReview": false }]
                }
                """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal("2026-03-05", body["date"]!.GetValue<string>());
        Assert.Equal("SquareMeter", body["items"]![0]!["unit"]!.GetValue<string>());
        Assert.Equal(18m, body["total"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Amount_IsRoundedToCents()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Шпакловка", 1.333m, Unit.SquareMeter, 7.50m)));

        var item = Assert.Single(day.Items);
        Assert.Equal(1.333m, item.Quantity);
        Assert.Equal(10.00m, item.Amount); // 9.9975
        Assert.Equal(10.00m, day.Total);
        Assert.Equal(0, day.OpenCount);
    }

    [Fact]
    public async Task List_NewestFirst_ByDateThenCreation_AndOnlyForThatSite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        await CreateDayAsync(owner, bath, Day(Today.AddDays(-5), Free("A"), "Стар"));
        await CreateDayAsync(owner, bath, Day(Today, Free("B"), "Днес 1"));
        await CreateDayAsync(owner, bath, Day(Today.AddDays(-1), Free("C"), "Вчера"));
        await CreateDayAsync(owner, bath, Day(Today, Free("D"), "Днес 2"));
        await CreateDayAsync(owner, kitchen, Day(Today, Free("E"), "Кухня"));

        var days = await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{bath}/workdays", Json);

        Assert.Equal(["Днес 2", "Днес 1", "Вчера", "Стар"], days!.Select(d => d.Title));
    }

    [Fact]
    public async Task List_SiteWithoutDays_IsEmpty()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        Assert.Empty((await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{siteId}/workdays", Json))!);
    }

    [Fact]
    public async Task Owner_Updates_ReplacesTheItems_KeepsTheIdsOfKeptItems()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(owner, siteId, Day(Today, [Free("Плочки", 2m, Unit.SquareMeter, 10m), Free("Боя")], "Ден"));
        var tiles = day.Items[0];

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/workdays/{day.Id}",
            new SaveWorkDayRequest(
                Today.AddDays(-2),
                null,
                "  Бележка  ",
                [
                    Free("Фуги", 3m, Unit.LinearMeter, 2.5m),
                    Free("Плочки", 4m, Unit.SquareMeter, 11m) with { Id = tiles.Id },
                ]),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json);
        Assert.NotNull(updated);
        Assert.Equal(day.Id, updated.Id);
        Assert.Equal(Today.AddDays(-2), updated.Date);
        Assert.Null(updated.Title);
        Assert.Equal("Бележка", updated.Note);
        Assert.Equal(["Фуги", "Плочки"], updated.Items.Select(i => i.Description));
        Assert.Equal(tiles.Id, updated.Items[1].Id);
        Assert.NotEqual(tiles.Id, updated.Items[0].Id);
        Assert.Equal([7.5m, 44m], updated.Items.Select(i => i.Amount));
        Assert.Equal(51.5m, updated.Total);
        Assert.Equal(0, updated.OpenCount);

        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        AssertSameDay(updated, read!);
    }

    [Fact]
    public async Task Owner_Deletes_Returns204_ThenGet404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки")));

        var response = await owner.DeleteAsync($"/api/sites/{siteId}/workdays/{day.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertNotFound(await owner.GetAsync($"/api/sites/{siteId}/workdays/{day.Id}"), DayNotFoundTitle);
        Assert.Empty((await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{siteId}/workdays", Json))!);
    }

    [Fact]
    public async Task UnknownSite_Returns404_OnListAndCreate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = Guid.NewGuid();

        await AssertNotFound(await owner.GetAsync($"/api/sites/{siteId}/workdays"), SiteNotFoundTitle);
        await AssertNotFound(
            await owner.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day(Today, Free("Плочки")), Json),
            SiteNotFoundTitle);
    }

    [Fact]
    public async Task UnknownDay_Returns404_OnGetPutDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var dayId = Guid.NewGuid();

        await AssertNotFound(await owner.GetAsync($"/api/sites/{siteId}/workdays/{dayId}"), DayNotFoundTitle);
        await AssertNotFound(
            await owner.PutAsJsonAsync($"/api/sites/{siteId}/workdays/{dayId}", Day(Today, Free("Плочки")), Json),
            DayNotFoundTitle);
        await AssertNotFound(await owner.DeleteAsync($"/api/sites/{siteId}/workdays/{dayId}"), DayNotFoundTitle);
    }

    [Fact]
    public async Task DayOfAnotherSite_Returns404_AndStaysUnchanged()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var day = await CreateDayAsync(owner, bath, Day(Today, Free("Плочки", 1m, Unit.Piece, 3m), "Баня"));

        await AssertNotFound(await owner.GetAsync($"/api/sites/{kitchen}/workdays/{day.Id}"), DayNotFoundTitle);
        await AssertNotFound(
            await owner.PutAsJsonAsync($"/api/sites/{kitchen}/workdays/{day.Id}", Day(Today, Free("Друго")), Json),
            DayNotFoundTitle);
        await AssertNotFound(await owner.DeleteAsync($"/api/sites/{kitchen}/workdays/{day.Id}"), DayNotFoundTitle);

        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{bath}/workdays/{day.Id}", Json);
        AssertSameDay(day, read!);
    }

    [Fact]
    public async Task FutureDate_Returns400OnDate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            Day(Today.AddDays(2), Free("Плочки")),
            Json);

        var problem = await AssertFieldError(response, "date");
        Assert.Equal([WorkDayRules.FutureDateMessage], problem.Errors["date"]);
    }

    [Fact]
    public async Task FutureDate_OnUpdate_Returns400OnDate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки")));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/workdays/{day.Id}",
            Day(Today.AddDays(2), Free("Плочки")),
            Json);

        await AssertFieldError(response, "date");
    }

    [Fact]
    public async Task NoItems_Returns400OnItems()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day(Today, []), Json);

        var problem = await AssertFieldError(response, "items");
        Assert.Equal([WorkDayRules.NoItemsMessage], problem.Errors["items"]);
    }

    [Fact]
    public async Task FiftyOneItems_Returns400OnItems()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            Day(Today, [.. Enumerable.Range(1, 51).Select(i => Free($"Позиция {i}"))]),
            Json);

        var problem = await AssertFieldError(response, "items");
        Assert.Equal([WorkDayRules.TooManyItemsMessage], problem.Errors["items"]);
    }

    [Theory]
    [InlineData("0", WorkDayRules.QuantityMustBePositiveMessage)]
    [InlineData("1.2345", WorkDayRules.QuantityTooManyDecimalsMessage)]
    public async Task BadQuantity_Returns400OnTheItemsQuantity(string quantity, string message)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsync(
            $"/api/sites/{siteId}/workdays",
            JsonBody($$"""
                {
                  "date": "{{Today:yyyy-MM-dd}}",
                  "items": [
                    { "description": "Плочки", "quantity": 1, "unit": "Piece", "needsReview": false },
                    { "description": "Боя", "quantity": {{quantity}}, "unit": "Piece", "needsReview": false }
                  ]
                }
                """));

        var problem = await AssertFieldError(response, "items[1].quantity");
        Assert.Equal([message], problem.Errors["items[1].quantity"]);
    }

    [Fact]
    public async Task MissingUnit_Returns400OnTheItemsUnit()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsync(
            $"/api/sites/{siteId}/workdays",
            JsonBody($$"""
                { "date": "{{Today:yyyy-MM-dd}}", "items": [{ "description": "Плочки", "quantity": 1 }] }
                """));

        await AssertFieldError(response, "items[0].unit");
    }

    [Fact]
    public async Task UnknownService_Returns400OnTheItemsServiceId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            Day(Today, [Free("Плочки"), Free("Боя") with { ServiceId = Guid.NewGuid() }]),
            Json);

        var problem = await AssertFieldError(response, "items[1].serviceId");
        Assert.Equal(["Няма такава услуга."], problem.Errors["items[1].serviceId"]);
        Assert.Empty((await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{siteId}/workdays", Json))!);
    }

    [Fact]
    public async Task Owner_TakesTheRateAsSent_EvenAgainstTheSitePrice()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var plaster = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter, 6m);
        await AddPriceAsync(owner, siteId, plaster.Id, 7m);

        var day = await CreateDayAsync(
            owner,
            siteId,
            Day(Today, [FromService(plaster, 2m, 8.25m), FromService(plaster, 1m, null)]));

        Assert.Equal([8.25m, null], day.Items.Select(i => i.Rate));
        Assert.Equal([plaster.Id, plaster.Id], day.Items.Select(i => i.ServiceId));
        Assert.Equal(16.5m, day.Total);
        Assert.Equal(1, day.OpenCount);
    }

    // Round 28: the Manager sets prices like the Owner; the server takes them as sent (also null).
    [Fact]
    public async Task Manager_TakesTheRatesAsSent_EvenAgainstTheSitePrice()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var manager = await CreateManagerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var plaster = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter, 6m);
        await AddPriceAsync(owner, siteId, plaster.Id, 7m);

        var day = await CreateDayAsync(
            manager,
            siteId,
            Day(Today, [FromService(plaster, 2m, 8.25m), FromService(plaster, 1m, null), Free("Извозване", 1m, Unit.Piece, 30m)]));

        Assert.Equal([8.25m, null, 30m], day.Items.Select(i => i.Rate));
        Assert.Equal(46.5m, day.Total);
        Assert.Equal(1, day.OpenCount);
    }

    [Fact]
    public async Task Manager_Update_ChangesTheRatesOfKeptAndNewItems()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var manager = await CreateManagerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var plaster = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter, 6m);

        var day = await CreateDayAsync(
            owner,
            siteId,
            Day(Today, [FromService(plaster, 2m, 9.5m), Free("Демонтаж", 1m, Unit.Day, null)]));

        var response = await manager.PutAsJsonAsync(
            $"/api/sites/{siteId}/workdays/{day.Id}",
            Day(Today,
            [
                FromService(plaster, 3m, 10m) with { Id = day.Items[0].Id, Description = "Шпакловка таван" },
                Free("Демонтаж", 2m, Unit.Day, 150m) with { Id = day.Items[1].Id, NeedsReview = true },
                FromService(plaster, 1m, null),
            ]),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json);
        Assert.NotNull(updated);
        Assert.Equal([day.Items[0].Id, day.Items[1].Id], updated.Items.Take(2).Select(i => i.Id));
        Assert.Equal(["Шпакловка таван", "Демонтаж", "Шпакловка"], updated.Items.Select(i => i.Description));
        Assert.Equal([10m, 150m, null], updated.Items.Select(i => i.Rate));
        Assert.Equal([false, true, false], updated.Items.Select(i => i.NeedsReview));
        Assert.Equal(330m, updated.Total);
        Assert.Equal(2, updated.OpenCount);
    }

    // An item id that is not an item of this day is a new item: new id, the price as sent,
    // and the other day's item is not touched.
    [Fact]
    public async Task ItemIdOfAnotherDay_IsTreatedAsANewItem()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var manager = await CreateManagerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var other = await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки", 1m, Unit.Piece, 50m), "Друг"));
        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Боя", 1m, Unit.Piece, 3m)));

        var response = await manager.PutAsJsonAsync(
            $"/api/sites/{siteId}/workdays/{day.Id}",
            Day(Today, Free("Плочки", 2m, Unit.Piece, 45m) with { Id = other.Items[0].Id }),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json);
        var item = Assert.Single(updated!.Items);
        Assert.NotEqual(other.Items[0].Id, item.Id);
        Assert.NotEqual(day.Items[0].Id, item.Id);
        Assert.Equal(45m, item.Rate);

        var otherRead = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{other.Id}", Json);
        AssertSameDay(other, otherRead!);
    }

    [Theory]
    [InlineData(SiteStatus.Lead, SiteStatus.Active)]
    [InlineData(SiteStatus.Active, SiteStatus.Active)]
    [InlineData(SiteStatus.Done, SiteStatus.Done)]
    public async Task NewDay_MakesALeadSiteActive_OthersStay(SiteStatus before, SiteStatus after)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня", before);

        await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки")));

        Assert.Equal(after, (await owner.GetFromJsonAsync<SiteResponse>($"/api/sites/{siteId}", Json))!.Status);
    }

    [Fact]
    public async Task UpdatingADay_DoesNotChangeTheSiteStatus()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки")));
        var back = await owner.PutAsJsonAsync($"/api/sites/{siteId}", new UpdateSiteRequest("Баня", SiteStatus.Lead), Json);
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);

        var update = await owner.PutAsJsonAsync($"/api/sites/{siteId}/workdays/{day.Id}", Day(Today, Free("Боя")), Json);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(SiteStatus.Lead, (await owner.GetFromJsonAsync<SiteResponse>($"/api/sites/{siteId}", Json))!.Status);
    }

    [Fact]
    public async Task DeleteSite_WithADay_Returns409_ThenWithoutDays_204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки")));

        var blocked = await owner.DeleteAsync($"/api/sites/{siteId}");

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("Обектът има записи", (await blocked.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/sites/{siteId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/sites/{siteId}/workdays/{day.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/sites/{siteId}")).StatusCode);
    }

    // Items keep their copy of the price; the link to the deleted service becomes null.
    [Fact]
    public async Task DeletedService_LeavesTheDayIntact_WithServiceIdNull()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var plaster = await CreateServiceAsync(owner, "Шпакловка", Unit.SquareMeter, 6m);
        var day = await CreateDayAsync(owner, siteId, Day(Today, FromService(plaster, 2m, 6m)));

        var delete = await owner.DeleteAsync($"/api/services/{plaster.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        var item = Assert.Single(read!.Items);
        Assert.Null(item.ServiceId);
        Assert.Equal("Шпакловка", item.Description);
        Assert.Equal(6m, item.Rate);
        Assert.Equal(12m, read.Total);
    }

    [Fact]
    public async Task Manager_CanCreateReadUpdate_ButNotDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var manager = await CreateManagerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(manager, siteId, Day(Today, Free("Плочки")));

        var list = await manager.GetAsync($"/api/sites/{siteId}/workdays");
        var get = await manager.GetAsync($"/api/sites/{siteId}/workdays/{day.Id}");
        var update = await manager.PutAsJsonAsync($"/api/sites/{siteId}/workdays/{day.Id}", Day(Today, Free("Боя")), Json);
        var delete = await manager.DeleteAsync($"/api/sites/{siteId}/workdays/{day.Id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/sites/{siteId}/workdays/{day.Id}")).StatusCode);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var day = await CreateDayAsync(owner, siteId, Day(Today, Free("Плочки")));
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync($"/api/sites/{siteId}/workdays");
        var create = await client.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day(Today, Free("Боя")), Json);
        var delete = await client.DeleteAsync($"/api/sites/{siteId}/workdays/{day.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    private static SaveWorkItemRequest Free(
        string description,
        decimal quantity = 1m,
        Unit unit = Unit.Piece,
        decimal? rate = null,
        bool needsReview = false)
        => new(null, null, description, quantity, unit, rate, needsReview);

    private static SaveWorkItemRequest FromService(ServiceResponse service, decimal quantity, decimal? rate)
        => new(null, service.Id, service.Name, quantity, service.Unit, rate, false);

    private static SaveWorkDayRequest Day(DateOnly date, SaveWorkItemRequest item, string? title = null)
        => new(date, title, null, [item]);

    private static SaveWorkDayRequest Day(DateOnly date, IReadOnlyList<SaveWorkItemRequest> items, string? title = null)
        => new(date, title, null, items);

    private static StringContent JsonBody(string json)
        => new(json, System.Text.Encoding.UTF8, "application/json");

    private static void AssertSameDay(WorkDayResponse expected, WorkDayResponse actual)
    {
        Assert.Equal(expected with { Items = [], Workers = [] }, actual with { Items = [], Workers = [] });
        Assert.Equal(expected.Items, actual.Items);
        Assert.Equal(expected.Workers, actual.Workers);
    }

    private async Task<HttpClient> CreateManagerClientAsync()
    {
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        return await factory.CreateLoggedInClientAsync("manager", Password);
    }

    private static async Task<WorkDayResponse> CreateDayAsync(HttpClient client, Guid siteId, SaveWorkDayRequest request)
    {
        var response = await client.PostAsJsonAsync($"/api/sites/{siteId}/workdays", request, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json);
        Assert.NotNull(day);
        return day;
    }

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name, SiteStatus? status = null)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name, status), Json);
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

    private static async Task AddPriceAsync(HttpClient owner, Guid siteId, Guid serviceId, decimal rate)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/prices",
            new CreateSitePriceRequest(serviceId, rate),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
