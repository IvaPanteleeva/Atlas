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
    private const string Password = "crew-password";

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();

        var list = await client.GetAsync("/api/users");
        var create = await client.PostAsJsonAsync("/api/users", Request("ivan", Roles.Crew));

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Crew_Gets403ProblemDetails()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("crew", Password, Roles.Crew);
        var client = await factory.CreateLoggedInClientAsync("crew", Password);

        var list = await client.GetAsync("/api/users");
        var create = await client.PostAsJsonAsync("/api/users", Request("ivan", Roles.Crew));

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("application/problem+json", list.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Owner_CreatesCrew_Returns201_AndCrewCanLogIn()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Crew));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(new UserResponse(user.Id, "ivan", Roles.Crew, null, null), user);
        Assert.Equal($"/api/users/{user.Id}", response.Headers.Location?.OriginalString);

        var crew = await factory.CreateLoggedInClientAsync("ivan", Password);
        var me = await crew.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal(new MeResponse(user.Id, "ivan", Roles.Crew, null), me);
    }

    [Fact]
    public async Task Owner_CreatesCyrillicUserName_Returns201()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("иван", Roles.Crew));

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
    public async Task Crew_WithCompany_Returns400()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/users",
            Request("ivan", Roles.Crew, CompanyConfiguration.ZhenistroyId));

        await AssertFieldError(response, "companyId");
    }

    [Fact]
    public async Task InvalidUserNameCharacters_Returns400_WithBulgarianMessageOnUserName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/users", Request("иван петров", Roles.Crew));

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
        await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Crew));

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
        await owner.PostAsJsonAsync("/api/users", Request("ivan", Roles.Crew));

        var users = await owner.GetFromJsonAsync<UserResponse[]>("/api/users");

        Assert.NotNull(users);
        Assert.Equal(
            [
                ("ivan", Roles.Crew, (Guid?)null, (string?)null),
                (AtlasApiFactory.TestOwnerUserName, Roles.Owner, null, null),
                ("zhenistroy", Roles.ClientViewer, CompanyConfiguration.ZhenistroyId, "ЖЕНИСТРОЙ ЕООД"),
            ],
            users.Select(u => (u.UserName, u.Role, u.CompanyId, u.CompanyName)));
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
