using System.Net;
using System.Net.Http.Json;
using Atlas.Application.Companies;
using Atlas.Application.Identity;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class CompanyEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "crew-password";

    private const string Zhenistroy = "ЖЕНИСТРОЙ ЕООД";

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();

        var list = await client.GetAsync("/api/companies");
        var create = await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Фирма Х"));
        var rename = await client.PutAsJsonAsync(
            $"/api/companies/{CompanyConfiguration.ZhenistroyId}",
            new UpdateCompanyRequest("Фирма Х"));
        var delete = await client.DeleteAsync($"/api/companies/{CompanyConfiguration.ZhenistroyId}");

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, rename.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task Owner_List_ReturnsZhenistroy()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var companies = await owner.GetFromJsonAsync<CompanyResponse[]>("/api/companies");

        Assert.NotNull(companies);
        Assert.Equal([new CompanyResponse(CompanyConfiguration.ZhenistroyId, Zhenistroy)], companies);
    }

    [Fact]
    public async Task List_IsOrderedByName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Я Строй"));
        await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("А Строй"));

        var companies = await owner.GetFromJsonAsync<CompanyResponse[]>("/api/companies");

        Assert.Equal(["А Строй", Zhenistroy, "Я Строй"], companies!.Select(c => c.Name));
    }

    [Fact]
    public async Task Crew_CanList_ButNotWrite()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("crew", Password, Roles.Crew);
        var crew = await factory.CreateLoggedInClientAsync("crew", Password);

        var list = await crew.GetAsync("/api/companies");
        var create = await crew.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Фирма Х"));
        var rename = await crew.PutAsJsonAsync(
            $"/api/companies/{CompanyConfiguration.ZhenistroyId}",
            new UpdateCompanyRequest("Фирма Х"));
        var delete = await crew.DeleteAsync($"/api/companies/{CompanyConfiguration.ZhenistroyId}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rename.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("application/problem+json", list.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_AndTrimmedName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest(" Фирма Х "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(company);
        Assert.Equal("Фирма Х", company.Name);
        Assert.Equal($"/api/companies/{company.Id}", response.Headers.Location?.OriginalString);

        var companies = await owner.GetFromJsonAsync<CompanyResponse[]>("/api/companies");
        Assert.Contains(new CompanyResponse(company.Id, "Фирма Х"), companies!);
    }

    [Theory]
    [InlineData(Zhenistroy)]
    [InlineData(" ЖЕНИСТРОЙ ЕООД  ")]
    [InlineData("Женистрой ЕООД")]
    [InlineData("женистрой еоод")]
    public async Task Create_DuplicateName_Returns409(string name)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest(name));

        await AssertConflict(response, "Фирма с това име вече има");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyName_Returns400(string name)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest(name));

        await AssertNameError(response);
    }

    [Fact]
    public async Task Create_NameLongerThan200_Returns400()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/companies",
            new CreateCompanyRequest(new string('а', CompanyRules.NameMaxLength + 1)));

        await AssertNameError(response);
    }

    [Fact]
    public async Task Owner_Renames_Returns200_WithTrimmedName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateAsync(owner, "Фирма Х");

        var response = await owner.PutAsJsonAsync(
            $"/api/companies/{company.Id}",
            new UpdateCompanyRequest(" Фирма Y "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new CompanyResponse(company.Id, "Фирма Y"), await response.Content.ReadFromJsonAsync<CompanyResponse>());
        var companies = await owner.GetFromJsonAsync<CompanyResponse[]>("/api/companies");
        Assert.Contains(new CompanyResponse(company.Id, "Фирма Y"), companies!);
        Assert.DoesNotContain(companies!, c => c.Name == "Фирма Х");
    }

    [Fact]
    public async Task Rename_ToItsOwnName_Returns200()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateAsync(owner, "Фирма Х");

        var response = await owner.PutAsJsonAsync($"/api/companies/{company.Id}", new UpdateCompanyRequest("Фирма Х"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Rename_ToExistingName_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateAsync(owner, "Фирма Х");

        var response = await owner.PutAsJsonAsync($"/api/companies/{company.Id}", new UpdateCompanyRequest(Zhenistroy));

        await AssertConflict(response, "Фирма с това име вече има");
    }

    [Fact]
    public async Task Rename_EmptyName_Returns400()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateAsync(owner, "Фирма Х");

        var response = await owner.PutAsJsonAsync($"/api/companies/{company.Id}", new UpdateCompanyRequest(" "));

        await AssertNameError(response);
    }

    [Fact]
    public async Task Rename_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PutAsJsonAsync($"/api/companies/{Guid.NewGuid()}", new UpdateCompanyRequest("Фирма Х"));

        await AssertNotFound(response);
    }

    [Fact]
    public async Task Owner_DeletesUnusedCompany_Returns204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateAsync(owner, "Фирма Х");

        var response = await owner.DeleteAsync($"/api/companies/{company.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var companies = await owner.GetFromJsonAsync<CompanyResponse[]>("/api/companies");
        Assert.DoesNotContain(companies!, c => c.Id == company.Id);
    }

    [Fact]
    public async Task Delete_CompanyWithClientViewer_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateAsync(owner, "Фирма Х");
        await factory.CreateUserAsync("firma-x", Password, Roles.ClientViewer, company.Id);

        var response = await owner.DeleteAsync($"/api/companies/{company.Id}");

        await AssertConflict(response, "Фирмата се използва");
        var companies = await owner.GetFromJsonAsync<CompanyResponse[]>("/api/companies");
        Assert.Contains(companies!, c => c.Id == company.Id);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.DeleteAsync($"/api/companies/{Guid.NewGuid()}");

        await AssertNotFound(response);
    }

    private static async Task<CompanyResponse> CreateAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(company);
        return company;
    }

    private static async Task AssertConflict(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Няма такава фирма.", problem?.Title);
    }

    private static async Task AssertNameError(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(["name"], problem.Errors.Keys);
    }
}
