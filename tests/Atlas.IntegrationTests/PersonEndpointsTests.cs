using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Identity;
using Atlas.Application.People;
using Atlas.Application.Sites;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class PersonEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private const string NotFoundTitle = "Няма такъв човек.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static DateOnly Today => WorkDayRules.Today(TimeProvider.System);

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();

        var list = await client.GetAsync("/api/people");
        var create = await client.PostAsJsonAsync("/api/people", Person("Иван"), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_TrimmedAndEmptyAsNull()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/people",
            new SavePersonRequest("  Иван Петров  ", "   ", "  Плочки  ", IsCrew: true, DailyRate: 85.50m),
            Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var person = await response.Content.ReadFromJsonAsync<PersonResponse>(Json);
        Assert.NotNull(person);
        Assert.Equal($"/api/people/{person.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(new PersonResponse(person.Id, "Иван Петров", null, "Плочки", true, 0, null, 85.50m, 0m, false, null), person);
    }

    [Fact]
    public async Task List_OrderedByName_DuplicatesAllowed_WithNotesForOwner()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await CreatePersonAsync(owner, Person("Петър", phone: "0888", note: "бояджия", isCrew: false));
        await CreatePersonAsync(owner, Person("Иван", note: "плочки"));
        await CreatePersonAsync(owner, Person("Иван"));

        var people = await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json);

        Assert.NotNull(people);
        Assert.Equal(["Иван", "Иван", "Петър"], people.Select(p => p.Name));
        Assert.Contains(people, p => p.Note == "плочки");
        Assert.Equal(("0888", "бояджия", false), (people[2].Phone, people[2].Note, people[2].IsCrew));
    }

    [Fact]
    public async Task Owner_Updates_ReplacesAllFields()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var person = await CreatePersonAsync(owner, Person("Иван", phone: "0888", note: "плочки"));

        var response = await owner.PutAsJsonAsync(
            $"/api/people/{person.Id}",
            new SavePersonRequest(" Иван П. ", null, "", IsCrew: false),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PersonResponse>(Json);
        Assert.Equal(new PersonResponse(person.Id, "Иван П.", null, null, false, 0, null, null, 0m, false, null), updated);
    }

    [Fact]
    public async Task Update_Unknown_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PutAsJsonAsync($"/api/people/{Guid.NewGuid()}", Person("Иван"), Json);

        await AssertNotFound(response);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        await AssertNotFound(await owner.DeleteAsync($"/api/people/{Guid.NewGuid()}"));
    }

    [Fact]
    public async Task EmptyName_Returns400OnName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/people", Person("   "), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["name"], problem?.Errors.Keys);
    }

    [Fact]
    public async Task Delete_ContactWithoutDays_Returns204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var contact = await CreatePersonAsync(owner, Person("Петър", isCrew: false));

        var response = await owner.DeleteAsync($"/api/people/{contact.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json))!);
    }

    [Fact]
    public async Task Delete_PersonWithWorkDays_Returns409_AndKeepsThePerson()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, Person("Иван"));
        var siteId = await CreateSiteAsync(owner, "Баня");
        await CreateDayAsync(owner, siteId, Today, ivan.Id);

        var response = await owner.DeleteAsync($"/api/people/{ivan.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Човекът има работни дни", problem?.Title);
        Assert.Single((await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json))!);
    }

    // Two sites on the same date are one day for the person (CLAUDE.md "Хора и профили").
    [Fact]
    public async Task DayCount_CountsDistinctDatesOverAllSites_AndLastWorkDate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, Person("Иван"));
        var petar = await CreatePersonAsync(owner, Person("Петър"));
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var twoDaysAgo = Today.AddDays(-2);
        var yesterday = Today.AddDays(-1);
        await CreateDayAsync(owner, bath, twoDaysAgo, ivan.Id, petar.Id);
        await CreateDayAsync(owner, kitchen, twoDaysAgo, ivan.Id);
        await CreateDayAsync(owner, bath, yesterday, ivan.Id);

        var people = await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json);

        Assert.NotNull(people);
        Assert.Equal((2, (DateOnly?)yesterday), (people[0].DayCount, people[0].LastWorkDate));
        Assert.Equal((1, (DateOnly?)twoDaysAgo), (people[1].DayCount, people[1].LastWorkDate));
    }

    // CLAUDE.md "Разход за хора": „изкарал“ = Σ the person's pay over all their days (days without a pay add nothing).
    [Fact]
    public async Task Earned_SumsThePayOverAllSites_DailyRateIsReturned()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, Person("Иван") with { DailyRate = 80m });
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        await CreateDayAsync(owner, bath, Today.AddDays(-2), [new SaveWorkerRequest(ivan.Id, 80m)]);
        await CreateDayAsync(owner, kitchen, Today.AddDays(-2), [new SaveWorkerRequest(ivan.Id, 40.50m)]);
        await CreateDayAsync(owner, bath, Today.AddDays(-1), [new SaveWorkerRequest(ivan.Id, null)]);

        var person = Assert.Single((await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json))!);

        Assert.Equal((2, (DateOnly?)Today.AddDays(-1), (decimal?)80m, 120.50m), (person.DayCount, person.LastWorkDate, person.DailyRate, person.Earned));
    }

    [Fact]
    public async Task BadDailyRate_Returns400OnDailyRate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/people", Person("Иван") with { DailyRate = 0m }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["dailyRate"], problem?.Errors.Keys);
    }

    [Fact]
    public async Task Manager_SeesNotes_CreatesAndUpdates_ButCannotDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, Person("Иван", phone: "0888", note: "плочки"));
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var list = await manager.GetAsync("/api/people");
        var create = await manager.PostAsJsonAsync("/api/people", Person("Петър", note: "бояджия"), Json);
        var update = await manager.PutAsJsonAsync($"/api/people/{ivan.Id}", Person("Иван", "0888", "плочки и фуги"), Json);
        var delete = await manager.DeleteAsync($"/api/people/{ivan.Id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var person = Assert.Single((await list.Content.ReadFromJsonAsync<PersonResponse[]>(Json))!);
        Assert.Equal(new PersonResponse(ivan.Id, "Иван", "0888", "плочки", true, 0, null, null, 0m, false, null), person);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal("бояджия", (await create.Content.ReadFromJsonAsync<PersonResponse>(Json))!.Note);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("плочки и фуги", (await update.Content.ReadFromJsonAsync<PersonResponse>(Json))!.Note);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(2, (await owner.GetFromJsonAsync<PersonResponse[]>("/api/people", Json))!.Length);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, Person("Иван"));
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync("/api/people");
        var create = await client.PostAsJsonAsync("/api/people", Person("Петър"), Json);
        var delete = await client.DeleteAsync($"/api/people/{ivan.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    private static SavePersonRequest Person(string name, string? phone = null, string? note = null, bool isCrew = true)
        => new(name, phone, note, isCrew);

    private static async Task<PersonResponse> CreatePersonAsync(HttpClient owner, SavePersonRequest request)
    {
        var response = await owner.PostAsJsonAsync("/api/people", request, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var person = await response.Content.ReadFromJsonAsync<PersonResponse>(Json);
        Assert.NotNull(person);
        return person;
    }

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static Task CreateDayAsync(HttpClient client, Guid siteId, DateOnly date, params Guid[] workerIds)
        => CreateDayAsync(client, siteId, date, [.. workerIds.Select(id => new SaveWorkerRequest(id, null))]);

    private static async Task CreateDayAsync(HttpClient client, Guid siteId, DateOnly date, SaveWorkerRequest[] workers)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            new SaveWorkDayRequest(
                date,
                null,
                null,
                [new SaveWorkItemRequest(null, null, "Плочки", 1m, Unit.Piece, 10m, false)],
                workers),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(NotFoundTitle, problem?.Title);
    }
}
