using System.Net;
using System.Net.Http.Json;
using Atlas.Application.Auth;
using Atlas.Application.Identity;
using Atlas.Application.Users;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class UserEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();

        var list = await client.GetAsync("/api/users");
        var create = await client.PostAsJsonAsync("/api/users", Request("ivan", Roles.Manager));

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Manager_Gets403ProblemDetails()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var client = await factory.CreateLoggedInClientAsync("manager", Password);

        var list = await client.GetAsync("/api/users");
        var create = await client.PostAsJsonAsync("/api/users", Request("ivan", Roles.Manager));

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("application/problem+json", list.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Owner_CreatesManager_Returns201_AndManagerCanLogIn()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Manager));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(new UserResponse(user.Id, "ivan", Roles.Manager, null, null), user);
        Assert.Equal($"/api/users/{user.Id}", response.Headers.Location?.OriginalString);

        var manager = await factory.CreateLoggedInClientAsync("ivan", Password);
        var me = await manager.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal(new MeResponse(user.Id, "ivan", Roles.Manager, null), me);
    }

    [Fact]
    public async Task Owner_CreatesCyrillicUserName_Returns201()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("иван", Roles.Manager));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await factory.CreateLoggedInClientAsync("иван", Password);
    }

    [Fact]
    public async Task Owner_CreatesOwner_Returns201()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("partner", Roles.Owner));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var partner = await factory.CreateLoggedInClientAsync("partner", Password);
        var list = await partner.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task Owner_CreatesClientViewer_WithZhenistroy_Returns201_AndMeHasCompanyId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/users",
            Request("zhenistroy", Roles.ClientViewer, CompanyConfiguration.ZhenistroyId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(CompanyConfiguration.ZhenistroyId, user?.CompanyId);
        Assert.Equal("ЖЕНИСТРОЙ ЕООД", user?.CompanyName);

        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal(Roles.ClientViewer, me?.Role);
        Assert.Equal(CompanyConfiguration.ZhenistroyId, me?.CompanyId);
    }

    [Fact]
    public async Task ClientViewer_WithoutCompany_Returns400()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("zhenistroy", Roles.ClientViewer));

        await AssertFieldError(response, "companyId");
    }

    [Fact]
    public async Task ClientViewer_WithNonExistingCompany_Returns400()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/users",
            Request("zhenistroy", Roles.ClientViewer, Guid.NewGuid()));

        await AssertFieldError(response, "companyId");
    }

    [Fact]
    public async Task Manager_WithCompany_Returns400()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/users",
            Request("ivan", Roles.Manager, CompanyConfiguration.ZhenistroyId));

        await AssertFieldError(response, "companyId");
    }

    // Round 28: the Crew role is gone (it became Manager).
    [Fact]
    public async Task CrewRole_Returns400OnRole()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("ivan", "Crew"));

        await AssertFieldError(response, "role");
    }

    [Fact]
    public async Task InvalidUserNameCharacters_Returns400_WithBulgarianMessageOnUserName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("иван петров", Roles.Manager));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(
            ["Потребителското име може да съдържа само букви (латиница или кирилица), цифри и - . _ @, без интервали."],
            problem.Errors["userName"]);
    }

    [Fact]
    public async Task DuplicateUserName_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Manager));

        var response = await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Owner));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Потребителското име е заето", problem?.Title);
    }

    [Fact]
    public async Task List_ReturnsUsersWithRolesAndCompany_OrderedByUserName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await owner.PostAsJsonAsync("/api/users", Request("zhenistroy", Roles.ClientViewer, CompanyConfiguration.ZhenistroyId));
        await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Manager));

        var users = await owner.GetFromJsonAsync<UserResponse[]>("/api/users");

        Assert.NotNull(users);
        Assert.Equal(
            [
                ("ivan", Roles.Manager, (Guid?)null, (string?)null),
                (AtlasApiFactory.TestOwnerUserName, Roles.Owner, null, null),
                ("zhenistroy", Roles.ClientViewer, CompanyConfiguration.ZhenistroyId, "ЖЕНИСТРОЙ ЕООД"),
            ],
            users.Select(u => (u.UserName, u.Role, u.CompanyId, u.CompanyName)));
    }

    [Fact]
    public async Task SetPassword_Returns204_OldPasswordFails_NewOneWorks()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await factory.CreateUserAsync("ivan", Password, Roles.Manager);

        var response = await owner.PutAsJsonAsync($"/api/users/{ivan.Id}/password", new SetPasswordRequest("new-password-1"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var oldLogin = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { userName = "ivan", password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        await factory.CreateLoggedInClientAsync("ivan", "new-password-1");
    }

    [Fact]
    public async Task SetPassword_ChangesTheSecurityStamp()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await factory.CreateUserAsync("ivan", Password, Roles.Manager);

        await owner.PutAsJsonAsync($"/api/users/{ivan.Id}/password", new SetPasswordRequest("new-password-1"));

        Assert.NotEqual(ivan.SecurityStamp, await factory.GetSecurityStampAsync(ivan.Id));
    }

    [Fact]
    public async Task SetPassword_TooShort_Returns400OnPassword_AndKeepsTheOldOne()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await factory.CreateUserAsync("ivan", Password, Roles.Manager);

        var response = await owner.PutAsJsonAsync($"/api/users/{ivan.Id}/password", new SetPasswordRequest("short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["password"], problem?.Errors.Keys);
        Assert.Contains("Парола", problem!.Errors["password"].Single(), StringComparison.Ordinal);
        await factory.CreateLoggedInClientAsync("ivan", Password);
    }

    [Fact]
    public async Task SetPassword_UnknownUser_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}/password", new SetPasswordRequest("new-password-1"));

        await AssertProblem(response, HttpStatusCode.NotFound, "Няма такъв профил.");
    }

    [Fact]
    public async Task Delete_Manager_Returns204_AndTheUserIsGone()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var ivan = await factory.CreateUserAsync("ivan", Password, Roles.Manager);

        var response = await owner.DeleteAsync($"/api/users/{ivan.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var users = await owner.GetFromJsonAsync<UserResponse[]>("/api/users");
        Assert.Equal([AtlasApiFactory.TestOwnerUserName], users!.Select(u => u.UserName));
        var login = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { userName = "ivan", password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        await AssertProblem(await owner.DeleteAsync($"/api/users/{Guid.NewGuid()}"), HttpStatusCode.NotFound, "Няма такъв профил.");
    }

    [Fact]
    public async Task Delete_Self_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var me = await owner.GetFromJsonAsync<MeResponse>("/api/auth/me");

        var response = await owner.DeleteAsync($"/api/users/{me!.Id}");

        await AssertProblem(response, HttpStatusCode.Conflict, "Не можеш да изтриеш собствения си профил.");
    }

    // With two Owners one can delete the other. The last Owner is protected: the deleted partner's session still
    // carries the Owner role until its cookie is checked against the security stamp again (at most a minute later,
    // AuthConfiguration), and within that minute it must not be able to delete the last Owner.
    [Fact]
    public async Task Delete_AnotherOwner_Returns204_ButTheLastOwnerReturns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var me = await owner.GetFromJsonAsync<MeResponse>("/api/auth/me");
        var partner = await factory.CreateUserAsync("partner", Password, Roles.Owner);
        var partnerClient = await factory.CreateLoggedInClientAsync("partner", Password);

        var deletePartner = await owner.DeleteAsync($"/api/users/{partner.Id}");
        var deleteLastOwner = await partnerClient.DeleteAsync($"/api/users/{me!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deletePartner.StatusCode);
        await AssertProblem(deleteLastOwner, HttpStatusCode.Conflict, "Трябва да остане поне един Owner.");
        var users = await owner.GetFromJsonAsync<UserResponse[]>("/api/users");
        Assert.Equal([AtlasApiFactory.TestOwnerUserName], users!.Select(u => u.UserName));
    }

    [Fact]
    public async Task Manager_CannotSetPasswordsOrDelete_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var managerUser = await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var client = await factory.CreateLoggedInClientAsync("manager", Password);

        var password = await client.PutAsJsonAsync($"/api/users/{managerUser.Id}/password", new SetPasswordRequest("new-password-1"));
        var delete = await client.DeleteAsync($"/api/users/{managerUser.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, password.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task ClientViewer_CannotSetPasswordsOrDelete_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var user = await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var password = await client.PutAsJsonAsync($"/api/users/{user.Id}/password", new SetPasswordRequest("new-password-1"));
        var delete = await client.DeleteAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, password.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
    }

    private static CreateUserRequest Request(string userName, string role, Guid? companyId = null)
        => new(userName, Password, role, companyId);

    private static async Task AssertFieldError(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(field, problem.Errors.Keys);
    }
}
