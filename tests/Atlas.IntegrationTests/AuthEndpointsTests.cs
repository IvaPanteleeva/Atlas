using System.Net;
using System.Net.Http.Json;
using Atlas.Application.Auth;
using Atlas.Application.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class AuthEndpointsTests(AtlasApiFactory factory)
{
    private const string WrongCredentialsTitle = "Грешно име или парола";

    private const string LockedOutTitle = "Твърде много опити. Опитай пак след 5 минути.";

    [Fact]
    public async Task Login_AsOwner_Returns200_SetsHttpOnlyLaxCookie_AndMeReturnsOwner()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(AtlasApiFactory.TestOwnerUserName, AtlasApiFactory.TestOwnerPassword));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await login.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(AtlasApiFactory.TestOwnerUserName, loggedIn?.UserName);
        Assert.Equal(Roles.Owner, loggedIn?.Role);

        var cookie = Assert.Single(
            login.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("atlas.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal(loggedIn, me);
        Assert.Null(me?.CompanyId);
    }

    [Fact]
    public async Task Login_WrongPassword_AndUnknownUser_Return401_WithTheSameTitle()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(AtlasApiFactory.TestOwnerUserName, "wrong-password"));
        var unknownUser = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("nobody-here", "wrong-password"));

        await AssertProblem(wrongPassword, HttpStatusCode.Unauthorized, WrongCredentialsTitle);
        await AssertProblem(unknownUser, HttpStatusCode.Unauthorized, WrongCredentialsTitle);
    }

    [Fact]
    public async Task Login_FiveWrongAttempts_LocksOut_EvenForTheCorrectPassword()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("lockout-crew", "right-password", Roles.Manager);
        var client = factory.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("lockout-crew", "wrong-password"));
        }

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("lockout-crew", "right-password"));

        await AssertProblem(response, HttpStatusCode.Unauthorized, LockedOutTitle);
    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401ProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(401, problem?.Status);
    }

    [Fact]
    public async Task Me_AfterLogout_Returns401()
    {
        await factory.ResetDatabaseAsync();
        var client = await factory.CreateOwnerClientAsync();

        var logout = await client.PostAsync("/api/auth/logout", null);
        var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLogin_Returns204()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400_WithFieldErrors()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["password", "userName"], problem?.Errors.Keys.Order(StringComparer.Ordinal));
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
    }
}
