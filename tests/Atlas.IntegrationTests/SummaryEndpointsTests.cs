using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Identity;
using Atlas.Application.Payments;
using Atlas.Application.Sites;
using Atlas.Application.Summaries;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

// The sums computed on the server (CLAUDE.md "Плащания и суми"): Earned, Paid, Remaining, OpenCount.
// Owner and Manager see them; the site list itself never carries money.
[Collection("Api")]
public class SummaryEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static DateOnly Today => WorkDayRules.Today(TimeProvider.System);

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/sites/{Guid.NewGuid()}/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sites/summaries")).StatusCode);
    }

    // The example from the brief, in three days:
    // Шпакловка 10 m² × 7.50 = 75.00; Боядисване 5 m² × 6.00, flagged = 30.00; Извозване 1 бр., no price.
    // Earned 105.00 (the flagged item with a price counts), OpenCount 2 (no price + flagged).
    [Fact]
    public async Task Example_Earned105_Open2_ThenPayments_ThenOverpaid()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Професорът");
        await CreateDayAsync(owner, siteId, Today.AddDays(-2), Item("Шпакловка", 10m, Unit.SquareMeter, 7.50m));
        await CreateDayAsync(owner, siteId, Today.AddDays(-1), Item("Боядисване", 5m, Unit.SquareMeter, 6.00m, needsReview: true));
        await CreateDayAsync(owner, siteId, Today, Item("Извозване", 1m, Unit.Piece, null));

        var noPayments = await GetSummaryAsync(owner, siteId);
        Assert.Equal(new SiteSummaryResponse(105.00m, 0m, 105.00m, 2, 3, Today), noPayments);

        await CreatePaymentAsync(owner, siteId, 50m);
        await CreatePaymentAsync(owner, siteId, 20m);

        var paid = await GetSummaryAsync(owner, siteId);
        Assert.Equal(new SiteSummaryResponse(105.00m, 70m, 35m, 2, 3, Today), paid);

        await CreatePaymentAsync(owner, siteId, 50m);

        var overpaid = await GetSummaryAsync(owner, siteId);
        Assert.Equal(new SiteSummaryResponse(105.00m, 120m, -15m, 2, 3, Today), overpaid);
    }

    // One rule: the site's Earned equals the sum of its days' Totals, and the open count matches too.
    [Fact]
    public async Task SiteSum_EqualsTheSumOfTheDayTotals_WithRounding()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await CreateDayAsync(owner, siteId, Today, Item("Шпакловка", 1.333m, Unit.SquareMeter, 7.50m), Item("Фуги", 0.125m, Unit.LinearMeter, 1m));
        await CreateDayAsync(owner, siteId, Today, Item("Фуги", 0.125m, Unit.LinearMeter, 1m), Item("Боя", 2m, Unit.SquareMeter, null));

        var days = (await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{siteId}/workdays", Json))!;
        var summary = await GetSummaryAsync(owner, siteId);

        Assert.Equal(10.26m, summary.Earned); // 10.00 + 0.13 + 0.13
        Assert.Equal(days.Sum(d => d.Total), summary.Earned);
        Assert.Equal(days.Sum(d => d.OpenCount), summary.OpenCount);
    }

    [Fact]
    public async Task SiteWithoutRecords_IsAllZero()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        Assert.Equal(new SiteSummaryResponse(0m, 0m, 0m, 0, 0, null), await GetSummaryAsync(owner, siteId));
    }

    // Sums do not depend on the status (CLAUDE.md "Обекти и фирми", round 21).
    [Theory]
    [InlineData(SiteStatus.Lead)]
    [InlineData(SiteStatus.Done)]
    public async Task Sums_CountAtEveryStatus(SiteStatus status)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await CreateDayAsync(owner, siteId, Today, Item("Плочки", 2m, Unit.SquareMeter, 10m));
        await CreatePaymentAsync(owner, siteId, 5m);
        var update = await owner.PutAsJsonAsync($"/api/sites/{siteId}", new UpdateSiteRequest("Баня", status), Json);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var summary = await GetSummaryAsync(owner, siteId);
        var all = await GetSummariesAsync(owner);

        Assert.Equal(new SiteSummaryResponse(20m, 5m, 15m, 0, 1, Today), summary);
        Assert.Equal(new SummaryTotalsResponse(20m, 5m, 15m, 0m, 0, 0), all.Totals);
    }

    [Fact]
    public async Task UnknownSite_Summary_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.GetAsync($"/api/sites/{Guid.NewGuid()}/summary");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Няма такъв обект.", problem?.Title);
    }

    // Two sites with records and one without. Earned, Paid and OpenCount are sums over the sites; „Дължат ни“ (Owed)
    // sums only the positive remainders: the kitchen overpaid by 30 does not lower the 35 the bath owes
    // (CLAUDE.md "Плащания и суми"). The overpayment is shown apart, with the number of sites.
    [Fact]
    public async Task Summaries_EverySite_AndTotalsAreTheSumsOverTheSites()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var empty = await CreateSiteAsync(owner, "Празен");
        await CreateDayAsync(owner, bath, Today.AddDays(-1), Item("Шпакловка", 10m, Unit.SquareMeter, 7.50m));
        await CreateDayAsync(owner, bath, Today, Item("Боядисване", 5m, Unit.SquareMeter, 6.00m, needsReview: true), Item("Извозване", 1m, Unit.Piece, null));
        await CreatePaymentAsync(owner, bath, 50m);
        await CreatePaymentAsync(owner, bath, 20m);
        await CreateDayAsync(owner, kitchen, Today, Item("Плочки", 1.333m, Unit.SquareMeter, 7.50m));
        await CreatePaymentAsync(owner, kitchen, 40m);

        var summaries = await GetSummariesAsync(owner);

        var bySite = summaries.Sites.ToDictionary(s => s.SiteId);
        Assert.Equal(3, summaries.Sites.Count);
        Assert.Equal(new SiteSummaryListResponse(bath, 105m, 70m, 35m, 2), bySite[bath]);
        Assert.Equal(new SiteSummaryListResponse(kitchen, 10m, 40m, -30m, 0), bySite[kitchen]);
        Assert.Equal(new SiteSummaryListResponse(empty, 0m, 0m, 0m, 0), bySite[empty]);
        Assert.Equal(new SummaryTotalsResponse(115m, 110m, 35m, 30m, 1, 2), summaries.Totals);
        Assert.Equal(summaries.Sites.Sum(s => s.Earned), summaries.Totals.Earned);
        Assert.Equal(summaries.Sites.Sum(s => s.Paid), summaries.Totals.Paid);
        Assert.Equal(summaries.Sites.Sum(s => Math.Max(s.Remaining, 0m)), summaries.Totals.Owed);
        Assert.Equal(summaries.Sites.Sum(s => Math.Max(-s.Remaining, 0m)), summaries.Totals.Overpaid);
        Assert.Equal(summaries.Sites.Sum(s => s.OpenCount), summaries.Totals.OpenCount);

        // The same numbers as the per-site summary.
        var bathSummary = await GetSummaryAsync(owner, bath);
        Assert.Equal(
            (bathSummary.Earned, bathSummary.Paid, bathSummary.Remaining, bathSummary.OpenCount),
            (bySite[bath].Earned, bySite[bath].Paid, bySite[bath].Remaining, bySite[bath].OpenCount));
    }

    [Fact]
    public async Task Summaries_WithoutSites_IsEmptyWithZeroTotals()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var summaries = await GetSummariesAsync(owner);

        Assert.Empty(summaries.Sites);
        Assert.Equal(new SummaryTotalsResponse(0m, 0m, 0m, 0m, 0, 0), summaries.Totals);
    }

    // "summaries" is not a guid, so it never reaches GET /api/sites/{id:guid} (which would be a 404 "Няма такъв обект.").
    [Fact]
    public async Task SummariesRoute_DoesNotCollideWithGetSiteById()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var summaries = await owner.GetAsync("/api/sites/summaries");
        var site = await owner.GetAsync($"/api/sites/{siteId}");

        Assert.Equal(HttpStatusCode.OK, summaries.StatusCode);
        var body = JsonNode.Parse(await summaries.Content.ReadAsStringAsync())!;
        var totals = body["totals"]!.AsObject();
        Assert.Equal(
            ["earned", "paid", "owed", "overpaid", "overpaidSiteCount", "openCount"],
            totals.Select(p => p.Key));
        Assert.Equal(HttpStatusCode.OK, site.StatusCode);
        Assert.Equal(siteId, (await site.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id);
    }

    [Fact]
    public async Task Manager_SeesSummaryAndSummaries_AndTheSiteListStaysWithoutMoney()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await CreateDayAsync(owner, siteId, Today, Item("Плочки", 2m, Unit.SquareMeter, 10m));
        await CreatePaymentAsync(owner, siteId, 5m);
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var summary = await manager.GetAsync($"/api/sites/{siteId}/summary");
        var summaries = await manager.GetAsync("/api/sites/summaries");
        var list = await manager.GetAsync("/api/sites");

        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var siteSummary = await summary.Content.ReadFromJsonAsync<SiteSummaryResponse>(Json);
        Assert.Equal((20m, 5m, 15m), (siteSummary!.Earned, siteSummary.Paid, siteSummary.Remaining));
        Assert.Equal(HttpStatusCode.OK, summaries.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var raw = await list.Content.ReadAsStringAsync();
        foreach (var money in new[] { "earned", "paid", "remaining", "amount", "total", "openCount" })
        {
            Assert.DoesNotContain(money, raw, StringComparison.OrdinalIgnoreCase);
        }

        var site = Assert.Single(JsonSerializer.Deserialize<SiteListResponse[]>(raw, Json)!);
        Assert.Equal(Today, site.LastWorkDate);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/sites/{siteId}/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/sites/summaries")).StatusCode);
    }

    private static SaveWorkItemRequest Item(
        string description,
        decimal quantity,
        Unit unit,
        decimal? rate,
        bool needsReview = false)
        => new(null, null, description, quantity, unit, rate, needsReview);

    private static async Task CreateDayAsync(
        HttpClient owner,
        Guid siteId,
        DateOnly date,
        params SaveWorkItemRequest[] items)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            new SaveWorkDayRequest(date, null, null, items),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task CreatePaymentAsync(HttpClient owner, Guid siteId, decimal amount)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/payments",
            new SavePaymentRequest(Today, amount, null),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name, SiteStatus.Active), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task<SiteSummaryResponse> GetSummaryAsync(HttpClient owner, Guid siteId)
    {
        var response = await owner.GetAsync($"/api/sites/{siteId}/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<SiteSummaryResponse>(Json);
        Assert.NotNull(summary);
        return summary;
    }

    private static async Task<SiteSummariesResponse> GetSummariesAsync(HttpClient owner)
    {
        var response = await owner.GetAsync("/api/sites/summaries");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summaries = await response.Content.ReadFromJsonAsync<SiteSummariesResponse>(Json);
        Assert.NotNull(summaries);
        return summaries;
    }
}
