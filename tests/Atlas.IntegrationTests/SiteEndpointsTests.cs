using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Companies;
using Atlas.Application.Identity;
using Atlas.Application.Sites;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

[Collection("Api")]
public class SiteEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private const string Zhenistroy = "ЖЕНИСТРОЙ ЕООД";

    private const string NotFoundTitle = "Няма такъв обект.";

    // Same as the API: enums travel as strings ("Lead", "Active", "Done").
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var id = Guid.NewGuid();

        var list = await client.GetAsync("/api/sites");
        var get = await client.GetAsync($"/api/sites/{id}");
        var create = await client.PostAsJsonAsync("/api/sites", new CreateSiteRequest("Баня"), Json);
        var update = await client.PutAsJsonAsync($"/api/sites/{id}", new UpdateSiteRequest("Баня", SiteStatus.Lead), Json);
        var delete = await client.DeleteAsync($"/api/sites/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var site = await CreateAsync(owner, new CreateSiteRequest("Баня", CompanyId: CompanyConfiguration.ZhenistroyId));
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync("/api/sites");
        var get = await client.GetAsync($"/api/sites/{site.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal("application/problem+json", list.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_Minimal_Returns201WithLocation_LeadAndNulls()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/sites", new { name = " Баня " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var site = await response.Content.ReadFromJsonAsync<SiteResponse>(Json);
        Assert.NotNull(site);
        Assert.Equal($"/api/sites/{site.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(new SiteResponse(site.Id, "Баня", SiteStatus.Lead, null, null, null, null, null, null), site);
    }

    [Fact]
    public async Task Create_Full_WithZhenistroy_ReturnsCompanyName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var site = await CreateAsync(owner, new CreateSiteRequest(
            "Апартамент Лозенец",
            SiteStatus.Active,
            CompanyConfiguration.ZhenistroyId,
            "Професорът",
            "ул. „Шипка“ 5",
            "https://maps.google.com/?q=42.69,23.32",
            "Ключът е при съседа"));

        Assert.Equal(
            new SiteResponse(
                site.Id,
                "Апартамент Лозенец",
                SiteStatus.Active,
                CompanyConfiguration.ZhenistroyId,
                Zhenistroy,
                "Професорът",
                "ул. „Шипка“ 5",
                "https://maps.google.com/?q=42.69,23.32",
                "Ключът е при съседа"),
            site);
    }

    [Fact]
    public async Task Create_TrimsText_AndEmptyStringsBecomeNull()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var site = await CreateAsync(owner, new CreateSiteRequest(
            "  Баня ",
            EndCustomer: "",
            Address: "   ",
            MapUrl: " ",
            Note: " Бележка "));

        Assert.Equal(new SiteResponse(site.Id, "Баня", SiteStatus.Lead, null, null, null, null, null, "Бележка"), site);
        var stored = await owner.GetFromJsonAsync<SiteResponse>($"/api/sites/{site.Id}", Json);
        Assert.Equal(site, stored);
    }

    [Fact]
    public async Task Create_StatusAsJsonString_IsAcceptedAndReturnedAsString()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/sites", new { name = "Баня", status = "Done" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal("Done", body["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task List_IsOrderedActiveLeadDone_ThenByName_WithoutNote()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        await CreateAsync(owner, new CreateSiteRequest("Б приключен", SiteStatus.Done));
        await CreateAsync(owner, new CreateSiteRequest("Б оглед", SiteStatus.Lead));
        await CreateAsync(owner, new CreateSiteRequest("Б текущ", SiteStatus.Active, Note: "вътрешно"));
        await CreateAsync(owner, new CreateSiteRequest("А приключен", SiteStatus.Done));
        await CreateAsync(owner, new CreateSiteRequest("А оглед", SiteStatus.Lead));
        await CreateAsync(owner, new CreateSiteRequest(
            "А текущ",
            SiteStatus.Active,
            CompanyConfiguration.ZhenistroyId,
            "Професорът",
            "ул. „Шипка“ 5"));

        var response = await owner.GetAsync("/api/sites");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("note", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("вътрешно", raw, StringComparison.Ordinal);

        var sites = JsonSerializer.Deserialize<SiteListResponse[]>(raw, Json)!;
        Assert.Equal(
            ["А текущ", "Б текущ", "А оглед", "Б оглед", "А приключен", "Б приключен"],
            sites.Select(s => s.Name));

        var first = sites[0];
        Assert.Equal(
            new SiteListResponse(first.Id, "А текущ", SiteStatus.Active, Zhenistroy, "Професорът", "ул. „Шипка“ 5", null),
            first);
    }

    // CLAUDE.md "Плащания и суми": current sites by their latest work day (newest first, without days last),
    // leads and finished ones by name. Group order stays Active, Lead, Done.
    [Fact]
    public async Task List_ActiveByLastWorkDate_NewestFirst_WithoutDaysLast_OthersByName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var today = WorkDayRules.Today(TimeProvider.System);
        var old = await CreateAsync(owner, new CreateSiteRequest("А стар", SiteStatus.Active));
        var recent = await CreateAsync(owner, new CreateSiteRequest("Я нов", SiteStatus.Active));
        await CreateAsync(owner, new CreateSiteRequest("Б без дни", SiteStatus.Active));
        await CreateAsync(owner, new CreateSiteRequest("А без дни", SiteStatus.Active));
        var middle = await CreateAsync(owner, new CreateSiteRequest("М среден", SiteStatus.Active));
        var lead = await CreateAsync(owner, new CreateSiteRequest("Я оглед", SiteStatus.Lead));
        await CreateAsync(owner, new CreateSiteRequest("А оглед", SiteStatus.Lead));
        var done = await CreateAsync(owner, new CreateSiteRequest("Я приключен", SiteStatus.Done));
        await CreateAsync(owner, new CreateSiteRequest("А приключен", SiteStatus.Done));

        await AddDayAsync(owner, old.Id, today.AddDays(-10));
        await AddDayAsync(owner, recent.Id, today.AddDays(-20));
        await AddDayAsync(owner, recent.Id, today);
        await AddDayAsync(owner, middle.Id, today.AddDays(-3));
        await AddDayAsync(owner, done.Id, today);

        // A new day on a Lead site makes it Active; set it back to Lead to check that leads ignore the date.
        await AddDayAsync(owner, lead.Id, today);
        var back = await owner.PutAsJsonAsync(
            $"/api/sites/{lead.Id}",
            new UpdateSiteRequest("Я оглед", SiteStatus.Lead),
            Json);
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);

        var sites = (await owner.GetFromJsonAsync<SiteListResponse[]>("/api/sites", Json))!;

        Assert.Equal(
            ["Я нов", "М среден", "А стар", "А без дни", "Б без дни", "А оглед", "Я оглед", "А приключен", "Я приключен"],
            sites.Select(s => s.Name));
        Assert.Equal(
            [today, today.AddDays(-3), today.AddDays(-10), null, null, null, today, null, today],
            sites.Select(s => s.LastWorkDate));
    }

    private static async Task AddDayAsync(HttpClient owner, Guid siteId, DateOnly date)
    {
        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/workdays",
            new SaveWorkDayRequest(date, null, null, [new SaveWorkItemRequest(null, null, "Плочки", 1m, Unit.Piece, 10m, false)]),
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsSiteWithNote()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var created = await CreateAsync(owner, new CreateSiteRequest("Баня", Note: "Ключът е при съседа"));

        var response = await owner.GetAsync($"/api/sites/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var site = await response.Content.ReadFromJsonAsync<SiteResponse>(Json);
        Assert.Equal(created, site);
        Assert.Equal("Ключът е при съседа", site!.Note);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.GetAsync($"/api/sites/{Guid.NewGuid()}");

        await AssertNotFound(response);
    }

    [Fact]
    public async Task Update_ChangesAllFields_Returns200()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateCompanyAsync(owner, "Фирма Х");
        var created = await CreateAsync(owner, new CreateSiteRequest(
            "Баня",
            SiteStatus.Lead,
            CompanyConfiguration.ZhenistroyId,
            "Професорът",
            "ул. „Шипка“ 5",
            "https://maps.google.com/?q=1",
            "Стара бележка"));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{created.Id}",
            new UpdateSiteRequest(
                " Кухня ",
                SiteStatus.Done,
                company.Id,
                "Доцентът",
                "бул. „Витоша“ 1",
                "http://maps.google.com/?q=2",
                "Нова бележка"),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expected = new SiteResponse(
            created.Id,
            "Кухня",
            SiteStatus.Done,
            company.Id,
            "Фирма Х",
            "Доцентът",
            "бул. „Витоша“ 1",
            "http://maps.google.com/?q=2",
            "Нова бележка");
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<SiteResponse>(Json));
        Assert.Equal(expected, await owner.GetFromJsonAsync<SiteResponse>($"/api/sites/{created.Id}", Json));
    }

    [Fact]
    public async Task Update_ClearsOptionalFields_WithEmptyStringsAndNullCompany()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var created = await CreateAsync(owner, new CreateSiteRequest(
            "Баня",
            SiteStatus.Active,
            CompanyConfiguration.ZhenistroyId,
            "Професорът",
            "ул. „Шипка“ 5",
            "https://maps.google.com/?q=1",
            "Бележка"));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{created.Id}",
            new UpdateSiteRequest("Баня", SiteStatus.Active, null, "", " ", "", "  "),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new SiteResponse(created.Id, "Баня", SiteStatus.Active, null, null, null, null, null, null),
            await response.Content.ReadFromJsonAsync<SiteResponse>(Json));
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{Guid.NewGuid()}",
            new UpdateSiteRequest("Баня", SiteStatus.Lead),
            Json);

        await AssertNotFound(response);
    }

    [Fact]
    public async Task Update_WithoutStatus_Returns400OnStatus()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var created = await CreateAsync(owner, new CreateSiteRequest("Баня"));

        var response = await owner.PutAsJsonAsync($"/api/sites/{created.Id}", new { name = "Баня" });

        await AssertFieldError(response, "status");
    }

    [Theory]
    [InlineData("maps.google.com")]
    [InlineData("ftp://x")]
    public async Task Create_InvalidMapUrl_Returns400OnMapUrl(string mapUrl)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest("Баня", MapUrl: mapUrl), Json);

        await AssertFieldError(response, "mapUrl");
    }

    [Fact]
    public async Task Update_InvalidMapUrl_Returns400OnMapUrl()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var created = await CreateAsync(owner, new CreateSiteRequest("Баня"));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{created.Id}",
            new UpdateSiteRequest("Баня", SiteStatus.Lead, MapUrl: "maps.google.com"),
            Json);

        await AssertFieldError(response, "mapUrl");
    }

    [Fact]
    public async Task Create_NameLongerThan200_Returns400OnName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/sites",
            new CreateSiteRequest(new string('а', SiteRules.NameMaxLength + 1)),
            Json);

        await AssertFieldError(response, "name");
    }

    [Fact]
    public async Task Create_UnknownCompany_Returns400OnCompanyId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.PostAsJsonAsync(
            "/api/sites",
            new CreateSiteRequest("Баня", CompanyId: Guid.NewGuid()),
            Json);

        var problem = await AssertFieldError(response, "companyId");
        Assert.Equal(["Няма такава фирма."], problem.Errors["companyId"]);
    }

    [Fact]
    public async Task Update_UnknownCompany_Returns400OnCompanyId()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var created = await CreateAsync(owner, new CreateSiteRequest("Баня"));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{created.Id}",
            new UpdateSiteRequest("Баня", SiteStatus.Lead, Guid.NewGuid()),
            Json);

        var problem = await AssertFieldError(response, "companyId");
        Assert.Equal(["Няма такава фирма."], problem.Errors["companyId"]);
    }

    [Fact]
    public async Task Manager_CanCreateAndUpdate_ButNotDelete()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var create = await manager.PostAsJsonAsync("/api/sites", new CreateSiteRequest("Баня"), Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var site = (await create.Content.ReadFromJsonAsync<SiteResponse>(Json))!;

        var list = await manager.GetAsync("/api/sites");
        var get = await manager.GetAsync($"/api/sites/{site.Id}");
        var update = await manager.PutAsJsonAsync(
            $"/api/sites/{site.Id}",
            new UpdateSiteRequest("Баня", SiteStatus.Active),
            Json);
        var delete = await manager.DeleteAsync($"/api/sites/{site.Id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"/api/sites/{site.Id}")).StatusCode);
    }

    [Fact]
    public async Task Owner_Deletes_Returns204_ThenGet404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var site = await CreateAsync(owner, new CreateSiteRequest("Баня"));

        var response = await owner.DeleteAsync($"/api/sites/{site.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertNotFound(await owner.GetAsync($"/api/sites/{site.Id}"));
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();

        var response = await owner.DeleteAsync($"/api/sites/{Guid.NewGuid()}");

        await AssertNotFound(response);
    }

    [Fact]
    public async Task DeleteCompany_WithSite_Returns409()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var company = await CreateCompanyAsync(owner, "Фирма Х");
        await CreateAsync(owner, new CreateSiteRequest("Баня", CompanyId: company.Id));

        var response = await owner.DeleteAsync($"/api/companies/{company.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Фирмата се използва", problem?.Title);
    }

    // The OpenAPI document shows SiteStatus as a string enum (JsonStringEnumConverter).
    [Fact]
    public async Task OpenApi_ShowsSiteStatus_AsStringEnum()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var schema = document["components"]!["schemas"]!["SiteStatus"]!;

        // .NET 10 writes a string enum as just "enum": [names] (no "type"); an integer enum would list numbers.
        Assert.All(schema["enum"]!.AsArray(), v => Assert.Equal(JsonValueKind.String, v!.GetValueKind()));
        Assert.Equal(
            ["Lead", "Active", "Done"],
            schema["enum"]!.AsArray().Select(v => v!.GetValue<string>()));
    }

    private static async Task<SiteResponse> CreateAsync(HttpClient client, CreateSiteRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/sites", request, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var site = await response.Content.ReadFromJsonAsync<SiteResponse>(Json);
        Assert.NotNull(site);
        return site;
    }

    private static async Task<CompanyResponse> CreateCompanyAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/companies", new CreateCompanyRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(company);
        return company;
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(NotFoundTitle, problem?.Title);
    }

    private static async Task<ValidationProblemDetails> AssertFieldError(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal([field], problem.Errors.Keys);
        return problem;
    }
}
