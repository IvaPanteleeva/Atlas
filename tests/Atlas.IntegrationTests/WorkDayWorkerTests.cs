using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Identity;
using Atlas.Application.People;
using Atlas.Application.Sites;
using Atlas.Application.WorkDays;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

// Who worked on a work day (CLAUDE.md "Хора и профили"): many-to-many WorkDay ↔ Person by id.
[Collection("Api")]
public class WorkDayWorkerTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static DateOnly Today => WorkDayRules.Today(TimeProvider.System);

    [Fact]
    public async Task Create_WithWorkers_ReturnsThemByName_WithTheirPayAsSent()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var petar = await CreatePersonAsync(owner, "Петър");
        var ivan = await CreatePersonAsync(owner, "Иван");

        var day = await CreateDayAsync(owner, siteId, [new SaveWorkerRequest(petar.Id, null), new SaveWorkerRequest(ivan.Id, 80m)]);

        Assert.Equal([new WorkerResponse(ivan.Id, "Иван", 80m, false), new WorkerResponse(petar.Id, "Петър", null, false)], day.Workers);
        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        Assert.Equal(day.Workers, read!.Workers);
        var list = await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{siteId}/workdays", Json);
        Assert.Equal(day.Workers, Assert.Single(list!).Workers);
    }

    [Fact]
    public async Task Create_WithoutWorkerIds_HasNoWorkers()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);

        var day = await CreateDayAsync(owner, siteId, (IReadOnlyList<SaveWorkerRequest>?)null);

        Assert.Empty(day.Workers);
    }

    [Fact]
    public async Task Create_WithContact_Returns400OnWorkerIds_AndSavesNothing()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var contact = await CreatePersonAsync(owner, "Петър", isCrew: false);

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            Day([ivan.Id, contact.Id]),
            Json);

        var problem = await AssertFieldError(response);
        Assert.Equal(["Човекът не е от екипа."], problem.Errors["workers"]);
        Assert.Empty((await owner.GetFromJsonAsync<WorkDayResponse[]>($"/api/sites/{siteId}/workdays", Json))!);
    }

    [Fact]
    public async Task Create_WithUnknownPerson_Returns400OnWorkerIds()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);

        var response = await owner.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day([Guid.NewGuid()]), Json);

        var problem = await AssertFieldError(response);
        Assert.Equal(["Няма такъв човек."], problem.Errors["workers"]);
    }

    // Someone taken off the crew stays on their old days, but cannot be put on a new one.
    [Fact]
    public async Task FormerCrew_StaysOnTheOldDayWhenEdited_ButCannotBeAddedAnywhere()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var petar = await CreatePersonAsync(owner, "Петър");
        var oldDay = await CreateDayAsync(owner, siteId, [ivan.Id]);
        var otherDay = await CreateDayAsync(owner, siteId, [petar.Id]);
        await UpdatePersonAsync(owner, ivan, isCrew: false);

        var keep = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/workdays/{oldDay.Id}",
            Day([ivan.Id, petar.Id], "Редакция"),
            Json);
        var addToOther = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/workdays/{otherDay.Id}",
            Day([petar.Id, ivan.Id]),
            Json);
        var addToNew = await owner.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day([ivan.Id]), Json);

        Assert.Equal(HttpStatusCode.OK, keep.StatusCode);
        var kept = await keep.Content.ReadFromJsonAsync<WorkDayResponse>(Json);
        Assert.Equal(["Иван", "Петър"], kept!.Workers.Select(w => w.Name));
        Assert.Equal(["Човекът не е от екипа."], (await AssertFieldError(addToOther)).Errors["workers"]);
        Assert.Equal(["Човекът не е от екипа."], (await AssertFieldError(addToNew)).Errors["workers"]);
    }

    [Fact]
    public async Task Update_ReplacesWorkers_NullKeepsThemWithTheirPay_EmptyClearsThem()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var petar = await CreatePersonAsync(owner, "Петър");
        var day = await CreateDayAsync(owner, siteId, [ivan.Id]);

        var replaced = await UpdateDayAsync(owner, siteId, day.Id, [new SaveWorkerRequest(petar.Id, 90m)]);
        var kept = await UpdateDayAsync(owner, siteId, day.Id, (IReadOnlyList<SaveWorkerRequest>?)null);
        var cleared = await UpdateDayAsync(owner, siteId, day.Id, Array.Empty<SaveWorkerRequest>());

        Assert.Equal([new WorkerResponse(petar.Id, "Петър", 90m, false)], replaced.Workers);
        Assert.Equal([new WorkerResponse(petar.Id, "Петър", 90m, false)], kept.Workers);
        Assert.Empty(cleared.Workers);
    }

    [Fact]
    public async Task Manager_SetsWorkers()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var petar = await CreatePersonAsync(owner, "Петър");
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var day = await CreateDayAsync(manager, siteId, [ivan.Id]);
        var updated = await UpdateDayAsync(manager, siteId, day.Id, [ivan.Id, petar.Id]);

        Assert.Equal(["Иван"], day.Workers.Select(w => w.Name));
        Assert.Equal(["Иван", "Петър"], updated.Workers.Select(w => w.Name));
    }

    [Fact]
    public async Task DeletingTheDay_KeepsThePeople()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var day = await CreateDayAsync(owner, siteId, [ivan.Id]);

        var delete = await owner.DeleteAsync($"/api/sites/{siteId}/workdays/{day.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var person = Assert.Single((await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json))!);
        Assert.Equal((ivan.Id, 0, (DateOnly?)null), (person.Id, person.DayCount, person.LastWorkDate));

        // With the day gone the person can be deleted.
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/people/{ivan.Id}")).StatusCode);
    }

    [Fact]
    public async Task RenamedPerson_ShowsTheNewNameOnTheDay()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var day = await CreateDayAsync(owner, siteId, [ivan.Id]);

        await owner.PutAsJsonAsync($"/api/people/{ivan.Id}", new SavePersonRequest("Иван Петров", null, null, true), Json);

        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        Assert.Equal([new WorkerResponse(ivan.Id, "Иван Петров", null, false)], read!.Workers);
    }

    [Fact]
    public async Task SamePersonTwice_Returns400OnWorkers()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            Day([new SaveWorkerRequest(ivan.Id, 80m), new SaveWorkerRequest(ivan.Id, 70m)]),
            Json);

        var problem = await AssertFieldError(response);
        Assert.Equal(["Човекът е избран два пъти."], problem.Errors["workers"]);
    }

    [Fact]
    public async Task BadPay_Returns400OnThatWorkersPay()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var petar = await CreatePersonAsync(owner, "Петър");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            Day([new SaveWorkerRequest(ivan.Id, 80m), new SaveWorkerRequest(petar.Id, 80.555m)]),
            Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["workers[1].pay"], problem!.Errors.Keys);
    }

    // The pay is a copy (CLAUDE.md "Разход за хора"): a new daily rate does not touch the days already saved.
    [Fact]
    public async Task ChangingTheDailyRate_DoesNotChangeTheOldDaysPay()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван", dailyRate: 80m);
        var day = await CreateDayAsync(owner, siteId, [new SaveWorkerRequest(ivan.Id, 80m)]);

        var update = await owner.PutAsJsonAsync(
            $"/api/people/{ivan.Id}",
            new SavePersonRequest("Иван", null, null, true, 100m),
            Json);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(100m, (await update.Content.ReadFromJsonAsync<PersonResponse>(Json))!.DailyRate);
        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        Assert.Equal([new WorkerResponse(ivan.Id, "Иван", 80m, false)], read!.Workers);
    }

    // The pay is stored as sent; the server does not fill it from the daily rate (the form does).
    [Fact]
    public async Task Pay_IsStoredAsSent_NotFromTheDailyRate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван", dailyRate: 80m);
        var petar = await CreatePersonAsync(owner, "Петър", dailyRate: 90m);

        var day = await CreateDayAsync(owner, siteId, [new SaveWorkerRequest(ivan.Id, 60m), new SaveWorkerRequest(petar.Id, null)]);

        Assert.Equal([60m, null], day.Workers.Select(w => w.Pay));
    }

    [Fact]
    public async Task Manager_SetsAndChangesPay()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var day = await CreateDayAsync(manager, siteId, [new SaveWorkerRequest(ivan.Id, 80m)]);
        var updated = await UpdateDayAsync(manager, siteId, day.Id, [new SaveWorkerRequest(ivan.Id, 95.50m)]);

        Assert.Equal([80m], day.Workers.Select(w => w.Pay));
        Assert.Equal([95.50m], updated.Workers.Select(w => w.Pay));
    }

    [Fact]
    public async Task ClientViewer_CannotWriteWorkers_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, Atlas.Infrastructure.Entities.CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var response = await client.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day([new SaveWorkerRequest(ivan.Id, 80m)]), Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Partners („съдружници“) may get a pay for the day („надник“, CLAUDE.md "Разход за хора", рунд 31); it is optional.
    [Fact]
    public async Task Partner_WithAPay_IsSaved_AndCountsInEarned()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var partner = await CreatePersonAsync(owner, "Ива");
        await factory.MakePartnerAsync(owner, partner.Id);

        var day = await CreateDayAsync(owner, siteId, [new SaveWorkerRequest(ivan.Id, 80m), new SaveWorkerRequest(partner.Id, 150m)]);

        Assert.Equal(
            [new WorkerResponse(partner.Id, "Ива", 150m, true), new WorkerResponse(ivan.Id, "Иван", 80m, false)],
            day.Workers);
        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        Assert.Equal(day.Workers, read!.Workers);
        var people = await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json);
        Assert.Equal(150m, people!.Single(p => p.Id == partner.Id).Earned);
    }

    [Fact]
    public async Task Partner_WithoutPay_IsOnTheDay_MarkedAsPartner_AndTheDayCounts()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var partner = await CreatePersonAsync(owner, "Ива");
        await factory.MakePartnerAsync(owner, partner.Id);

        var day = await CreateDayAsync(owner, siteId, [new SaveWorkerRequest(ivan.Id, 80m), new SaveWorkerRequest(partner.Id, null)]);

        Assert.Equal(
            [new WorkerResponse(partner.Id, "Ива", null, true), new WorkerResponse(ivan.Id, "Иван", 80m, false)],
            day.Workers);
        var people = await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json);
        var stored = people!.Single(p => p.Id == partner.Id);
        Assert.Equal((true, 1, 0m), (stored.IsPartner, stored.DayCount, stored.Earned));
        Assert.NotNull(stored.LinkedUserName);
    }

    // Made a partner later (linked to an Owner profile): the pay already stored on an old day stays, and it may be changed
    // like any other pay.
    [Fact]
    public async Task NowPartner_KeepsTheStoredPayOnTheOldDay_AndCanChangeIt()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var day = await CreateDayAsync(owner, siteId, [new SaveWorkerRequest(ivan.Id, 80m)]);
        await factory.MakePartnerAsync(owner, ivan.Id, Roles.Owner);

        var read = await owner.GetFromJsonAsync<WorkDayResponse>($"/api/sites/{siteId}/workdays/{day.Id}", Json);
        var changed = await UpdateDayAsync(owner, siteId, day.Id, [new SaveWorkerRequest(ivan.Id, 90m)]);

        Assert.Equal([new WorkerResponse(ivan.Id, "Иван", 80m, true)], read!.Workers);
        Assert.Equal([new WorkerResponse(ivan.Id, "Иван", 90m, true)], changed.Workers);
    }

    // Workers without a pay, by id.
    private static SaveWorkDayRequest Day(IReadOnlyList<Guid>? workerIds, string? title = null)
        => Day(workerIds?.Select(id => new SaveWorkerRequest(id, null)).ToList(), title);

    private static SaveWorkDayRequest Day(IReadOnlyList<SaveWorkerRequest>? workers, string? title = null)
        => new(
            Today,
            title,
            null,
            [new SaveWorkItemRequest(null, null, "Плочки", 1m, Unit.Piece, 10m, false)],
            workers);

    private static async Task<Guid> CreateSiteAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest("Баня"), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task<PersonResponse> CreatePersonAsync(
        HttpClient owner,
        string name,
        bool isCrew = true,
        decimal? dailyRate = null)
    {
        var response = await owner.PostAsJsonAsync(
            "/api/people",
            new SavePersonRequest(name, null, null, isCrew, dailyRate),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PersonResponse>(Json))!;
    }

    private static async Task UpdatePersonAsync(HttpClient owner, PersonResponse person, bool isCrew)
    {
        var response = await owner.PutAsJsonAsync(
            $"/api/people/{person.Id}",
            new SavePersonRequest(person.Name, person.Phone, person.Note, isCrew, person.DailyRate),
            Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<WorkDayResponse> CreateDayAsync(HttpClient client, Guid siteId, IReadOnlyList<Guid>? workerIds)
        => CreateDayAsync(client, siteId, workerIds?.Select(id => new SaveWorkerRequest(id, null)).ToList());

    private static async Task<WorkDayResponse> CreateDayAsync(
        HttpClient client,
        Guid siteId,
        IReadOnlyList<SaveWorkerRequest>? workers)
    {
        var response = await client.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day(workers), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json))!;
    }

    private static Task<WorkDayResponse> UpdateDayAsync(
        HttpClient client,
        Guid siteId,
        Guid dayId,
        IReadOnlyList<Guid>? workerIds)
        => UpdateDayAsync(client, siteId, dayId, workerIds?.Select(id => new SaveWorkerRequest(id, null)).ToList());

    private static async Task<WorkDayResponse> UpdateDayAsync(
        HttpClient client,
        Guid siteId,
        Guid dayId,
        IReadOnlyList<SaveWorkerRequest>? workers)
    {
        var response = await client.PutAsJsonAsync($"/api/sites/{siteId}/workdays/{dayId}", Day(workers), Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json))!;
    }

    private static async Task<ValidationProblemDetails> AssertFieldError(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(["workers"], problem.Errors.Keys);
        return problem;
    }
}
