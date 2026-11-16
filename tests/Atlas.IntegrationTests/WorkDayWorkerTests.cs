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
    public async Task Create_WithWorkers_ReturnsThemByName_DuplicatesCountOnce()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var petar = await CreatePersonAsync(owner, "Петър");
        var ivan = await CreatePersonAsync(owner, "Иван");

        var day = await CreateDayAsync(owner, siteId, [petar.Id, ivan.Id, petar.Id]);

        Assert.Equal([new WorkerResponse(ivan.Id, "Иван"), new WorkerResponse(petar.Id, "Петър")], day.Workers);
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

        var day = await CreateDayAsync(owner, siteId, null);

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
        Assert.Equal(["Човекът не е от екипа."], problem.Errors["workerIds"]);
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
        Assert.Equal(["Няма такъв човек."], problem.Errors["workerIds"]);
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
        Assert.Equal(["Човекът не е от екипа."], (await AssertFieldError(addToOther)).Errors["workerIds"]);
        Assert.Equal(["Човекът не е от екипа."], (await AssertFieldError(addToNew)).Errors["workerIds"]);
    }

    [Fact]
    public async Task Update_ReplacesWorkers_NullKeepsThem_EmptyClearsThem()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner);
        var ivan = await CreatePersonAsync(owner, "Иван");
        var petar = await CreatePersonAsync(owner, "Петър");
        var day = await CreateDayAsync(owner, siteId, [ivan.Id]);

        var replaced = await UpdateDayAsync(owner, siteId, day.Id, [petar.Id]);
        var kept = await UpdateDayAsync(owner, siteId, day.Id, null);
        var cleared = await UpdateDayAsync(owner, siteId, day.Id, []);

        Assert.Equal(["Петър"], replaced.Workers.Select(w => w.Name));
        Assert.Equal(["Петър"], kept.Workers.Select(w => w.Name));
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
        Assert.Equal([new WorkerResponse(ivan.Id, "Иван Петров")], read!.Workers);
    }

    private static SaveWorkDayRequest Day(IReadOnlyList<Guid>? workerIds, string? title = null)
        => new(
            Today,
            title,
            null,
            [new SaveWorkItemRequest(null, null, "Плочки", 1m, Unit.Piece, 10m, false)],
            workerIds);

    private static async Task<Guid> CreateSiteAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest("Баня"), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task<PersonResponse> CreatePersonAsync(HttpClient owner, string name, bool isCrew = true)
    {
        var response = await owner.PostAsJsonAsync("/api/people", new SavePersonRequest(name, null, null, isCrew), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PersonResponse>(Json))!;
    }

    private static async Task UpdatePersonAsync(HttpClient owner, PersonResponse person, bool isCrew)
    {
        var response = await owner.PutAsJsonAsync(
            $"/api/people/{person.Id}",
            new SavePersonRequest(person.Name, person.Phone, person.Note, isCrew),
            Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<WorkDayResponse> CreateDayAsync(HttpClient client, Guid siteId, IReadOnlyList<Guid>? workerIds)
    {
        var response = await client.PostAsJsonAsync($"/api/sites/{siteId}/workdays", Day(workerIds), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json))!;
    }

    private static async Task<WorkDayResponse> UpdateDayAsync(
        HttpClient client,
        Guid siteId,
        Guid dayId,
        IReadOnlyList<Guid>? workerIds)
    {
        var response = await client.PutAsJsonAsync($"/api/sites/{siteId}/workdays/{dayId}", Day(workerIds), Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkDayResponse>(Json))!;
    }

    private static async Task<ValidationProblemDetails> AssertFieldError(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(["workerIds"], problem.Errors.Keys);
        return problem;
    }
}
