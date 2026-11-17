using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atlas.Application.Auth;
using Atlas.Application.Identity;
using Atlas.Application.People;
using Atlas.Application.Sites;
using Atlas.Application.Tasks;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

// Tasks (CLAUDE.md "Задачи"): of a site or of the company; Owner and Manager add, edit and tick them off,
// only the Owner deletes. The person responsible is one of the crew.
[Collection("Api")]
public class TaskEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private const string TaskNotFoundTitle = "Няма такава задача.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/tasks", NewTask("Лепило"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync($"/api/tasks/{id}/done", new SetTaskDoneRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
    }

    [Fact]
    public async Task Owner_CreatesCompanyTask_Returns201WithLocation_Trimmed_AndTheContractFields()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var response = await owner.PostAsJsonAsync("/api/tasks", NewTask("  Купи лепило  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ["assigneeId", "assigneeName", "createdAt", "done", "doneAt", "id", "siteId", "text"],
            json.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));

        var task = json.Deserialize<TaskResponse>(Json)!;
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(
            ("Купи лепило", false, (Guid?)null, (Guid?)null, (string?)null, (DateTimeOffset?)null),
            (task.Text, task.Done, task.SiteId, task.AssigneeId, task.AssigneeName, task.DoneAt));
        Assert.InRange(task.CreatedAt, before, DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Create_OnASite_WithACrewMember_ReturnsTheNameOfThePersonResponsible()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var ivan = await CreatePersonAsync(owner, "Иван", isCrew: true);

        var task = await CreateTaskAsync(owner, NewTask("Плочки", siteId, ivan.Id));

        Assert.Equal((siteId, ivan.Id, "Иван"), (task.SiteId, task.AssigneeId, task.AssigneeName));
    }

    [Fact]
    public async Task List_FiltersBySite_ByPerson_AndCompanyScope_WithoutParametersAll()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var ivan = await CreatePersonAsync(owner, "Иван", isCrew: true);
        await CreateTaskAsync(owner, NewTask("Фирма 1"));
        await CreateTaskAsync(owner, NewTask("Фирма 2", assigneeId: ivan.Id));
        await CreateTaskAsync(owner, NewTask("Баня 1", bath, ivan.Id));
        await CreateTaskAsync(owner, NewTask("Кухня 1", kitchen));

        Assert.Equal(["Фирма 1", "Фирма 2", "Баня 1", "Кухня 1"], await ListAsync(owner, ""));
        Assert.Equal(["Баня 1"], await ListAsync(owner, $"?siteId={bath}"));
        Assert.Equal(["Фирма 2", "Баня 1"], await ListAsync(owner, $"?assigneeId={ivan.Id}"));
        Assert.Equal(["Фирма 1", "Фирма 2"], await ListAsync(owner, "?scope=company"));
        Assert.Equal(["Фирма 2"], await ListAsync(owner, $"?scope=company&assigneeId={ivan.Id}"));
        Assert.Empty(await ListAsync(owner, $"?siteId={Guid.NewGuid()}"));
    }

    [Fact]
    public async Task List_UnknownScope_Returns400OnScope()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.GetAsync("/api/tasks?scope=everything");

        await AssertFieldError(response, "scope", TaskRules.UnknownScopeMessage);
    }

    // Open first, the oldest first; then the ticked ones, the last ticked first.
    [Fact]
    public async Task List_OpenByCreatedAt_ThenDoneByDoneAtNewestFirst()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var first = await CreateTaskAsync(owner, NewTask("Първа"));
        var second = await CreateTaskAsync(owner, NewTask("Втора"));
        await CreateTaskAsync(owner, NewTask("Трета"));
        var fourth = await CreateTaskAsync(owner, NewTask("Четвърта"));
        await SetDoneAsync(owner, second.Id, true);
        await SetDoneAsync(owner, first.Id, true);
        await CreateTaskAsync(owner, NewTask("Пета"));

        Assert.Equal(["Трета", "Четвърта", "Пета", "Първа", "Втора"], await ListAsync(owner, ""));

        await SetDoneAsync(owner, fourth.Id, true);
        await SetDoneAsync(owner, first.Id, false);

        Assert.Equal(["Първа", "Трета", "Пета", "Четвърта", "Втора"], await ListAsync(owner, ""));
    }

    [Fact]
    public async Task SetDone_SetsDoneAt_AndTakingTheTickAwayClearsIt()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var task = await CreateTaskAsync(owner, NewTask("Лепило"));
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var done = await SetDoneAsync(owner, task.Id, true);

        Assert.True(done.Done);
        Assert.NotNull(done.DoneAt);
        Assert.InRange(done.DoneAt.Value, before, DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(task.CreatedAt, done.CreatedAt);

        var reopened = await SetDoneAsync(owner, task.Id, false);

        Assert.Equal(task, reopened);
        var listed = Assert.Single((await owner.GetFromJsonAsync<TaskResponse[]>("/api/tasks"))!);
        Assert.Equal((false, (DateTimeOffset?)null), (listed.Done, listed.DoneAt));
    }

    [Fact]
    public async Task SetDone_WithoutDone_Returns400OnDone()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var task = await CreateTaskAsync(owner, NewTask("Лепило"));

        var response = await owner.PutAsJsonAsync($"/api/tasks/{task.Id}/done", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["done"], problem?.Errors.Keys);
    }

    [Fact]
    public async Task Update_ReplacesTextAndPersonResponsible_KeepsSiteAndTick()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var ivan = await CreatePersonAsync(owner, "Иван", isCrew: true);
        var petar = await CreatePersonAsync(owner, "Петър", isCrew: true);
        var task = await CreateTaskAsync(owner, NewTask("Плочки", siteId, ivan.Id));
        var done = await SetDoneAsync(owner, task.Id, true);

        var response = await owner.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest("  Фаянс  ", petar.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            done with { Text = "Фаянс", AssigneeId = petar.Id, AssigneeName = "Петър" },
            await response.Content.ReadFromJsonAsync<TaskResponse>());

        var unassigned = await owner.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest("Фаянс", null));
        Assert.Equal(HttpStatusCode.OK, unassigned.StatusCode);
        Assert.Equal(((Guid?)null, (string?)null), Assignee(await unassigned.Content.ReadFromJsonAsync<TaskResponse>()));
    }

    [Fact]
    public async Task Create_PersonResponsibleNotCrewOrUnknown_Returns400OnAssigneeId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var contact = await CreatePersonAsync(owner, "Майстор Гошо", isCrew: false);

        await AssertFieldError(
            await owner.PostAsJsonAsync("/api/tasks", NewTask("Лепило", assigneeId: contact.Id)),
            "assigneeId",
            "Отговорникът трябва да е от екипа.");
        await AssertFieldError(
            await owner.PostAsJsonAsync("/api/tasks", NewTask("Лепило", assigneeId: Guid.NewGuid())),
            "assigneeId",
            "Няма такъв човек.");
        Assert.Empty(await ListAsync(owner, ""));
    }

    // The current person responsible stays even when no longer crew; a new choice must be crew.
    [Fact]
    public async Task Update_PersonResponsibleNoLongerCrew_StaysOnTheTask_ButCannotBeChosenAgain()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, "Иван", isCrew: true);
        var petar = await CreatePersonAsync(owner, "Петър", isCrew: true);
        var task = await CreateTaskAsync(owner, NewTask("Лепило", assigneeId: ivan.Id));
        var other = await CreateTaskAsync(owner, NewTask("Плочки"));
        var off = await owner.PutAsJsonAsync($"/api/people/{ivan.Id}", new SavePersonRequest("Иван", null, null, false));
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);

        var keep = await owner.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest("Лепило и фуга", ivan.Id));
        Assert.Equal(HttpStatusCode.OK, keep.StatusCode);
        Assert.Equal((ivan.Id, "Иван"), Assignee(await keep.Content.ReadFromJsonAsync<TaskResponse>()));

        await AssertFieldError(
            await owner.PutAsJsonAsync($"/api/tasks/{other.Id}", new UpdateTaskRequest("Плочки", ivan.Id)),
            "assigneeId",
            "Отговорникът трябва да е от екипа.");

        // Changed to someone else, the old person responsible is a new choice again.
        var change = await owner.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest("Лепило", petar.Id));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        await AssertFieldError(
            await owner.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest("Лепило", ivan.Id)),
            "assigneeId",
            "Отговорникът трябва да е от екипа.");
    }

    [Fact]
    public async Task Create_UnknownSite_Returns400OnSiteId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/tasks", NewTask("Лепило", Guid.NewGuid()));

        await AssertFieldError(response, "siteId", "Няма такъв обект.");
    }

    [Fact]
    public async Task EmptyOrTooLongText_Returns400OnText()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var task = await CreateTaskAsync(owner, NewTask("Лепило"));

        var create = await owner.PostAsJsonAsync("/api/tasks", NewTask("   "));
        var update = await owner.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest(new string('а', 501)));

        foreach (var response in new[] { create, update })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.Equal(["text"], problem?.Errors.Keys);
        }
    }

    [Fact]
    public async Task UnknownTask_Returns404_OnUpdateDoneAndDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var id = Guid.NewGuid();

        await AssertNotFound(await owner.PutAsJsonAsync($"/api/tasks/{id}", new UpdateTaskRequest("Лепило")));
        await AssertNotFound(await owner.PutAsJsonAsync($"/api/tasks/{id}/done", new SetTaskDoneRequest(true)));
        await AssertNotFound(await owner.DeleteAsync($"/api/tasks/{id}"));
    }

    [Fact]
    public async Task Owner_Deletes_Returns204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var task = await CreateTaskAsync(owner, NewTask("Лепило"));
        await CreateTaskAsync(owner, NewTask("Плочки"));

        var response = await owner.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Плочки"], await ListAsync(owner, ""));
    }

    [Fact]
    public async Task DeletingThePersonResponsible_LeavesTheTasksWithoutOne()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var ivan = await CreatePersonAsync(owner, "Иван", isCrew: true);
        await CreateTaskAsync(owner, NewTask("Лепило", assigneeId: ivan.Id));
        await CreateTaskAsync(owner, NewTask("Плочки", siteId, ivan.Id));

        var response = await owner.DeleteAsync($"/api/people/{ivan.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var tasks = await owner.GetFromJsonAsync<TaskResponse[]>("/api/tasks");
        Assert.NotNull(tasks);
        Assert.Equal(["Лепило", "Плочки"], tasks.Select(t => t.Text));
        Assert.All(tasks, t => Assert.Equal(((Guid?)null, (string?)null), (t.AssigneeId, t.AssigneeName)));
    }

    // Tasks are not history: they do not block deleting the site and go with it; the company's tasks stay.
    [Fact]
    public async Task DeletingTheSite_WithTasks_Returns204_AndItsTasksAreGone()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        await CreateTaskAsync(owner, NewTask("Фирма"));
        await CreateTaskAsync(owner, NewTask("Баня 1", bath));
        var bathDone = await CreateTaskAsync(owner, NewTask("Баня 2", bath));
        await SetDoneAsync(owner, bathDone.Id, true);
        await CreateTaskAsync(owner, NewTask("Кухня 1", kitchen));

        var response = await owner.DeleteAsync($"/api/sites/{bath}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Фирма", "Кухня 1"], await ListAsync(owner, ""));
        Assert.Empty(await ListAsync(owner, $"?siteId={bath}"));
    }

    [Fact]
    public async Task Manager_ReadsCreatesUpdatesTicks_ButCannotDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var create = await manager.PostAsJsonAsync("/api/tasks", NewTask("Лепило"));
        var task = await create.Content.ReadFromJsonAsync<TaskResponse>();
        var list = await manager.GetAsync("/api/tasks");
        var update = await manager.PutAsJsonAsync($"/api/tasks/{task!.Id}", new UpdateTaskRequest("Лепило и фуга"));
        var done = await manager.PutAsJsonAsync($"/api/tasks/{task.Id}/done", new SetTaskDoneRequest(true));
        var delete = await manager.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(["Лепило и фуга"], await ListAsync(owner, ""));
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var task = await CreateTaskAsync(owner, NewTask("Лепило"));
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/tasks", NewTask("Плочки"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest("Плочки"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync($"/api/tasks/{task.Id}/done", new SetTaskDoneRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
    }

    // „Мои“ in the tasks tab: /me says which person the profile is linked to, also right after the link changes.
    [Fact]
    public async Task Me_ReturnsThePersonLinkedToTheProfile()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await CreatePersonAsync(owner, "Иван", isCrew: true);
        var user = await factory.CreateUserAsync("ivan", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("ivan", Password);

        Assert.Null((await manager.GetFromJsonAsync<MeResponse>("/api/auth/me"))!.PersonId);

        var link = await owner.PutAsJsonAsync($"/api/users/{user.Id}/person", new { personId = ivan.Id });
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);

        Assert.Equal(ivan.Id, (await manager.GetFromJsonAsync<MeResponse>("/api/auth/me"))!.PersonId);

        var login = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest("ivan", Password));
        Assert.Equal(ivan.Id, (await login.Content.ReadFromJsonAsync<MeResponse>())!.PersonId);
    }

    private static CreateTaskRequest NewTask(string text, Guid? siteId = null, Guid? assigneeId = null)
        => new(text, siteId, assigneeId);

    private static (Guid?, string?) Assignee(TaskResponse? task)
        => (task?.AssigneeId, task?.AssigneeName);

    private static async Task<string[]> ListAsync(HttpClient client, string query)
        => [.. (await client.GetFromJsonAsync<TaskResponse[]>($"/api/tasks{query}"))!.Select(t => t.Text)];

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task<PersonResponse> CreatePersonAsync(HttpClient owner, string name, bool isCrew)
    {
        var response = await owner.PostAsJsonAsync("/api/people", new SavePersonRequest(name, null, null, isCrew));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PersonResponse>())!;
    }

    private static async Task<TaskResponse> CreateTaskAsync(HttpClient client, CreateTaskRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }

    private static async Task<TaskResponse> SetDoneAsync(HttpClient client, Guid id, bool done)
    {
        var response = await client.PutAsJsonAsync($"/api/tasks/{id}/done", new SetTaskDoneRequest(done));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }

    private static async Task AssertFieldError(HttpResponseMessage response, string field, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal([field], problem.Errors.Keys);
        Assert.Equal([message], problem.Errors[field]);
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(TaskNotFoundTitle, problem?.Title);
    }
}
