using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Companies;
using Atlas.Application.Identity;
using Atlas.Application.Payments;
using Atlas.Application.People;
using Atlas.Application.Sites;
using Atlas.Application.Summaries;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

// The client portal (CLAUDE.md "Достъп за възложителя", рунд 33): a ClientViewer sees only the sites of its own
// company (any status), the days with their items, the payments (date and amount) and the same sums as inside.
// Notes, people, worker sums, „Остава за нас“, contacts, tasks and site prices never leave the server.
[Collection("Api")]
public class PortalEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private const string NotFoundTitle = "Няма такъв обект.";

    private const string ClientUserName = "zhenistroy";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static DateOnly Today => WorkDayRules.Today(TimeProvider.System);

    private static Guid CompanyA => CompanyConfiguration.ZhenistroyId;

    [Fact]
    public async Task Anonymous_Gets401()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/portal/sites")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/portal/sites/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task OwnerAndManager_Get403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня", CompanyA);
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        foreach (var staff in new[] { owner, manager })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/portal/sites")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync($"/portal/sites/{siteId}")).StatusCode);
        }
    }

    // A ClientViewer must belong to a company; without one there is nothing to filter by, so nothing is shown.
    [Fact]
    public async Task ClientViewerWithoutCompany_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня", CompanyA);
        await factory.CreateUserAsync("no-company", Password, Roles.ClientViewer);
        var client = await factory.CreateLoggedInClientAsync("no-company", Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/portal/sites")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/portal/sites/{siteId}")).StatusCode);
    }

    [Fact]
    public async Task ClientViewer_OnInternalApi_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня", CompanyA);
        var client = await CreateClientViewerAsync(CompanyA);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/sites")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/sites/{siteId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/sites/{siteId}/workdays")).StatusCode);
    }

    // The core rule: a user of company A never sees a site of company B, nor a site without a company.
    // The answer is the same 404 as for an id that does not exist (it does not reveal that the site is there).
    [Fact]
    public async Task OtherCompanysSite_AndSiteWithoutCompany_Return404_AndAreNotListed()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var companyB = await CreateCompanyAsync(owner, "Друга фирма");
        var own = await CreateSiteAsync(owner, "Нашият", CompanyA);
        var foreign = await CreateSiteAsync(owner, "Чужд", companyB);
        var direct = await CreateSiteAsync(owner, "Директен", null);
        await CreateDayAsync(owner, foreign, Today, Item("Плочки", 1m, Unit.Piece, 100m));
        await CreateDayAsync(owner, direct, Today, Item("Плочки", 1m, Unit.Piece, 100m));
        var client = await CreateClientViewerAsync(CompanyA);

        var list = await GetSitesAsync(client);
        var site = Assert.Single(list["sites"]!.AsArray());
        Assert.Equal(own, site!["id"]!.GetValue<Guid>());

        foreach (var hidden in new[] { foreign, direct, Guid.NewGuid() })
        {
            var response = await client.GetAsync($"/portal/sites/{hidden}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal(NotFoundTitle, problem?.Title);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/portal/sites/{own}")).StatusCode);

        // And the other way around: the user of company B sees only its own site.
        var clientB = await CreateClientViewerAsync(companyB, "firma-b");
        var listB = await GetSitesAsync(clientB);
        Assert.Equal(foreign, Assert.Single(listB["sites"]!.AsArray())!["id"]!.GetValue<Guid>());
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/portal/sites/{own}")).StatusCode);
    }

    // Every status is shown (leads too), in the order of the internal list: current, leads, finished.
    [Fact]
    public async Task EveryStatus_IsListed_InTheOrderOfTheInternalList()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var done = await CreateSiteAsync(owner, "А приключен", CompanyA);
        await SetStatusAsync(owner, done, "А приключен", SiteStatus.Done);
        var lead = await CreateSiteAsync(owner, "Б оглед", CompanyA);
        var older = await CreateSiteAsync(owner, "В текущ", CompanyA);
        await CreateDayAsync(owner, older, Today.AddDays(-3), Item("Плочки", 1m, Unit.Piece, 10m));
        var newer = await CreateSiteAsync(owner, "Г текущ", CompanyA);
        await CreateDayAsync(owner, newer, Today, Item("Плочки", 1m, Unit.Piece, 10m));
        var client = await CreateClientViewerAsync(CompanyA);

        var internalOrder = (await owner.GetFromJsonAsync<SiteListResponse[]>("/api/sites", Json))!.Select(s => s.Id);
        var sites = (await GetSitesAsync(client))["sites"]!.AsArray();

        Assert.Equal([newer, older, lead, done], sites.Select(s => s!["id"]!.GetValue<Guid>()));
        Assert.Equal(internalOrder, sites.Select(s => s!["id"]!.GetValue<Guid>()));
        Assert.Equal(["Active", "Active", "Lead", "Done"], sites.Select(s => s!["status"]!.GetValue<string>()));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/portal/sites/{lead}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/portal/sites/{done}")).StatusCode);
    }

    // The list: exactly the contract's fields, the same sums as /api/sites/summaries, and totals of the company only.
    // „Дължите“ (owed) sums only the positive remainders: the overpaid kitchen does not lower what the bath owes.
    [Fact]
    public async Task List_SumsAndTotals_LikeTheInternalSummaries_OnlyForTheCompany()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var companyB = await CreateCompanyAsync(owner, "Друга фирма");
        var bath = await CreateSiteAsync(owner, "Баня", CompanyA, endCustomer: "Професорът");
        var kitchen = await CreateSiteAsync(owner, "Кухня", CompanyA);
        var foreign = await CreateSiteAsync(owner, "Чужд", companyB);
        await CreateDayAsync(owner, bath, Today.AddDays(-1), Item("Шпакловка", 10m, Unit.SquareMeter, 7.50m));
        await CreateDayAsync(
            owner,
            bath,
            Today,
            Item("Боядисване", 5m, Unit.SquareMeter, 6.00m, needsReview: true),
            Item("Извозване", 1m, Unit.Piece, null));
        await CreatePaymentAsync(owner, bath, 70m);
        await CreateDayAsync(owner, kitchen, Today, Item("Плочки", 1.333m, Unit.SquareMeter, 7.50m));
        await CreatePaymentAsync(owner, kitchen, 40m);
        await CreateDayAsync(owner, foreign, Today, Item("Плочки", 1m, Unit.Piece, 1000m));
        var client = await CreateClientViewerAsync(CompanyA);

        var body = await GetSitesAsync(client);

        Assert.Equal(["sites", "totals"], Keys(body));
        var totals = body["totals"]!.AsObject();
        Assert.Equal(["owed", "overpaid", "overpaidSiteCount"], Keys(totals));
        Assert.Equal((35m, 30m, 1), (Dec(totals["owed"]), Dec(totals["overpaid"]), totals["overpaidSiteCount"]!.GetValue<int>()));

        var sites = body["sites"]!.AsArray().Select(s => s!.AsObject()).ToDictionary(s => s["id"]!.GetValue<Guid>());
        Assert.Equal([bath, kitchen], sites.Keys.Order());
        foreach (var site in sites.Values)
        {
            Assert.Equal(["id", "name", "endCustomer", "status", "earned", "paid", "remaining", "openCount"], Keys(site));
        }

        Assert.Equal("Баня", sites[bath]["name"]!.GetValue<string>());
        Assert.Equal("Професорът", sites[bath]["endCustomer"]!.GetValue<string>());
        Assert.Null(sites[kitchen]["endCustomer"]);

        var internalSums = (await owner.GetFromJsonAsync<SiteSummariesResponse>("/api/sites/summaries", Json))!
            .Sites.ToDictionary(s => s.SiteId);
        foreach (var id in new[] { bath, kitchen })
        {
            var expected = internalSums[id];
            Assert.Equal(
                (expected.Earned, expected.Paid, expected.Remaining, expected.OpenCount),
                (Dec(sites[id]["earned"]), Dec(sites[id]["paid"]), Dec(sites[id]["remaining"]), sites[id]["openCount"]!.GetValue<int>()));
        }

        Assert.Equal((105m, 70m, 35m, 2), (Dec(sites[bath]["earned"]), Dec(sites[bath]["paid"]), Dec(sites[bath]["remaining"]), sites[bath]["openCount"]!.GetValue<int>()));
        Assert.Equal((10m, 40m, -30m, 0), (Dec(sites[kitchen]["earned"]), Dec(sites[kitchen]["paid"]), Dec(sites[kitchen]["remaining"]), sites[kitchen]["openCount"]!.GetValue<int>()));
    }

    [Fact]
    public async Task List_WithoutSites_IsEmptyWithZeroTotals()
    {
        await factory.ResetDatabaseAsync();
        var client = await CreateClientViewerAsync(CompanyA);

        var body = await GetSitesAsync(client);

        Assert.Empty(body["sites"]!.AsArray());
        var totals = body["totals"]!;
        Assert.Equal((0m, 0m, 0), (Dec(totals["owed"]), Dec(totals["overpaid"]), totals["overpaidSiteCount"]!.GetValue<int>()));
    }

    // The detail: exactly the contract's fields at every level, nothing internal in the raw JSON,
    // items without a price and flagged items shown like inside, and the same sums as /api/sites/{id}/summary.
    [Fact]
    public async Task Detail_ShowsOnlyTheContractFields_AndTheSameSumsAsInside()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(
            owner,
            "Баня",
            CompanyA,
            endCustomer: "Професорът",
            address: "ул. Шипка 1",
            mapUrl: "https://maps.example/1",
            note: "ТАЙНА-БЕЛЕЖКА-ОБЕКТ");
        var ivan = await CreatePersonAsync(owner, "ТАЙНО-ИМЕ-ИВАН");
        await CreateDayAsync(owner, siteId, Today.AddDays(-2), Item("Шпакловка", 10m, Unit.SquareMeter, 7.50m));
        var newest = await CreateDayAsync(
            owner,
            siteId,
            Today,
            "Баня – ден 2",
            "ТАЙНА-БЕЛЕЖКА-ДЕН",
            [new SaveWorkerRequest(ivan, 80m)],
            Item("Боядисване", 5m, Unit.SquareMeter, 6.00m, needsReview: true),
            Item("Извозване", 1m, Unit.Piece, null));
        await CreatePaymentAsync(owner, siteId, 50m, Today.AddDays(-1), "ТАЙНА-БЕЛЕЖКА-ПЛАЩАНЕ");
        await CreatePaymentAsync(owner, siteId, 20m, Today, null);
        await CreateContactAsync(owner, siteId, "ТАЙНО-ИМЕ-КОНТАКТ");
        await CreateTaskAsync(owner, siteId, "ТАЙНА-ЗАДАЧА");
        var client = await CreateClientViewerAsync(CompanyA);

        var response = await client.GetAsync($"/portal/sites/{siteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        foreach (var secret in new[] { "ТАЙНА", "ТАЙНО", "note", "worker", "\"pay\"", "\"net\"", "cost", "contact", "task", "price\"", "serviceId", "phone" })
        {
            Assert.DoesNotContain(secret, raw, StringComparison.OrdinalIgnoreCase);
        }

        var site = JsonNode.Parse(raw)!.AsObject();
        Assert.Equal(["id", "name", "endCustomer", "address", "mapUrl", "status", "summary", "workDays", "payments"], Keys(site));
        Assert.Equal(siteId, site["id"]!.GetValue<Guid>());
        Assert.Equal("Баня", site["name"]!.GetValue<string>());
        Assert.Equal("Професорът", site["endCustomer"]!.GetValue<string>());
        Assert.Equal("ул. Шипка 1", site["address"]!.GetValue<string>());
        Assert.Equal("https://maps.example/1", site["mapUrl"]!.GetValue<string>());
        Assert.Equal("Active", site["status"]!.GetValue<string>());

        var summary = site["summary"]!.AsObject();
        Assert.Equal(["earned", "paid", "remaining", "openCount"], Keys(summary));
        var inside = (await owner.GetFromJsonAsync<SiteSummaryResponse>($"/api/sites/{siteId}/summary", Json))!;
        Assert.Equal(
            (inside.Earned, inside.Paid, inside.Remaining, inside.OpenCount),
            (Dec(summary["earned"]), Dec(summary["paid"]), Dec(summary["remaining"]), summary["openCount"]!.GetValue<int>()));
        Assert.Equal((105m, 70m, 35m, 2), (inside.Earned, inside.Paid, inside.Remaining, inside.OpenCount));

        // Newest day first; the items in their own order.
        var days = site["workDays"]!.AsArray().Select(d => d!.AsObject()).ToList();
        Assert.Equal(2, days.Count);
        Assert.All(days, d => Assert.Equal(["id", "date", "title", "total", "items"], Keys(d)));
        Assert.Equal(newest, days[0]["id"]!.GetValue<Guid>());
        Assert.Equal(Iso(Today), days[0]["date"]!.GetValue<string>());
        Assert.Equal(Iso(Today.AddDays(-2)), days[1]["date"]!.GetValue<string>());
        Assert.Equal("Баня – ден 2", days[0]["title"]!.GetValue<string>());
        Assert.Null(days[1]["title"]);
        Assert.Equal(30m, Dec(days[0]["total"]));
        Assert.Equal(75m, Dec(days[1]["total"]));

        var items = days[0]["items"]!.AsArray().Select(i => i!.AsObject()).ToList();
        Assert.All(items, i => Assert.Equal(["description", "quantity", "unit", "rate", "needsReview", "amount"], Keys(i)));
        Assert.Equal(
            ("Боядисване", 5m, "SquareMeter", 6.00m, true, 30.00m),
            (items[0]["description"]!.GetValue<string>(), Dec(items[0]["quantity"]), items[0]["unit"]!.GetValue<string>(),
                Dec(items[0]["rate"]), items[0]["needsReview"]!.GetValue<bool>(), Dec(items[0]["amount"])));
        Assert.Equal("Извозване", items[1]["description"]!.GetValue<string>());
        Assert.Equal("Piece", items[1]["unit"]!.GetValue<string>());
        Assert.Null(items[1]["rate"]);
        Assert.Null(items[1]["amount"]);
        Assert.False(items[1]["needsReview"]!.GetValue<bool>());

        // Payments: only date and amount, newest first like the internal list.
        var payments = site["payments"]!.AsArray().Select(p => p!.AsObject()).ToList();
        Assert.All(payments, p => Assert.Equal(["date", "amount"], Keys(p)));
        Assert.Equal(
            [(Iso(Today), 20m), (Iso(Today.AddDays(-1)), 50m)],
            payments.Select(p => (p["date"]!.GetValue<string>(), Dec(p["amount"]))));
    }

    [Fact]
    public async Task Detail_LeadSiteWithoutRecords_IsEmptyWithZeroSums()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Оглед", CompanyA);
        var client = await CreateClientViewerAsync(CompanyA);

        var site = (await client.GetFromJsonAsync<JsonObject>($"/portal/sites/{siteId}"))!;

        Assert.Equal("Lead", site["status"]!.GetValue<string>());
        Assert.Null(site["endCustomer"]);
        Assert.Null(site["address"]);
        Assert.Null(site["mapUrl"]);
        Assert.Empty(site["workDays"]!.AsArray());
        Assert.Empty(site["payments"]!.AsArray());
        var summary = site["summary"]!;
        Assert.Equal(
            (0m, 0m, 0m, 0),
            (Dec(summary["earned"]), Dec(summary["paid"]), Dec(summary["remaining"]), summary["openCount"]!.GetValue<int>()));
    }

    private static IEnumerable<string> Keys(JsonObject node) => node.Select(p => p.Key);

    private static decimal Dec(JsonNode? node) => node!.GetValue<decimal>();

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static SaveWorkItemRequest Item(
        string description,
        decimal quantity,
        Unit unit,
        decimal? rate,
        bool needsReview = false)
        => new(null, null, description, quantity, unit, rate, needsReview);

    private async Task<HttpClient> CreateClientViewerAsync(Guid companyId, string userName = ClientUserName)
    {
        await factory.CreateUserAsync(userName, Password, Roles.ClientViewer, companyId);
        return await factory.CreateLoggedInClientAsync(userName, Password);
    }

    private static async Task<JsonObject> GetSitesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/portal/sites");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
    }

    private static async Task<Guid> CreateCompanyAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest(name), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompanyResponse>(Json))!.Id;
    }

    private static async Task<Guid> CreateSiteAsync(
        HttpClient owner,
        string name,
        Guid? companyId,
        string? endCustomer = null,
        string? address = null,
        string? mapUrl = null,
        string? note = null)
    {
        var response = await owner.PostAsJsonAsync(
            "/api/sites",
            new CreateSiteRequest(name, null, companyId, endCustomer, address, mapUrl, note),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task SetStatusAsync(HttpClient owner, Guid siteId, string name, SiteStatus status)
    {
        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}",
            new UpdateSiteRequest(name, status, CompanyA),
            Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<Guid> CreateDayAsync(HttpClient owner, Guid siteId, DateOnly date, params SaveWorkItemRequest[] items)
        => CreateDayAsync(owner, siteId, date, null, null, null, items);

    private static async Task<Guid> CreateDayAsync(
        HttpClient owner,
        Guid siteId,
        DateOnly date,
        string? title,
        string? note,
        IReadOnlyList<SaveWorkerRequest>? workers,
        params SaveWorkItemRequest[] items)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            new SaveWorkDayRequest(date, title, note, items, workers),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json))!.Id;
    }

    private static async Task<Guid> CreatePersonAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/people", new SavePersonRequest(name, null, "ТАЙНА-БЕЛЕЖКА-ЧОВЕК", true), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PersonResponse>(Json))!.Id;
    }

    private static Task CreatePaymentAsync(HttpClient owner, Guid siteId, decimal amount)
        => CreatePaymentAsync(owner, siteId, amount, Today, null);

    private static async Task CreatePaymentAsync(HttpClient owner, Guid siteId, decimal amount, DateOnly date, string? note)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/payments",
            new SavePaymentRequest(date, amount, note),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task CreateContactAsync(HttpClient owner, Guid siteId, string name)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/contacts",
            new { name, role = "съсед", phone = "0888123456", note = "ТАЙНА-БЕЛЕЖКА-КОНТАКТ" },
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task CreateTaskAsync(HttpClient owner, Guid siteId, string text)
    {
        var response = await owner.PostAsJsonAsync("/api/tasks", new { siteId, text }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
