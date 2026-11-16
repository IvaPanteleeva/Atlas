using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atlas.Application.Identity;
using Atlas.Application.SiteContacts;
using Atlas.Application.Sites;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

// Contacts of a site (CLAUDE.md "Контакти на обекта"): Owner and Manager add and edit, only the Owner deletes.
[Collection("Api")]
public class SiteContactEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "user-password";

    private const string SiteNotFoundTitle = "Няма такъв обект.";

    private const string ContactNotFoundTitle = "Няма такъв контакт.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var siteId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/sites/{siteId}/contacts");
        var create = await client.PostAsJsonAsync($"/api/sites/{siteId}/contacts", Contact("Петров"));

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_TrimmedAndEmptyAsNull()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/contacts",
            new SaveSiteContactRequest("  Г-н Петров  ", " собственик ", "   ", "  звъни след 18  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var contact = await response.Content.ReadFromJsonAsync<SiteContactResponse>();
        Assert.NotNull(contact);
        Assert.Equal($"/api/sites/{siteId}/contacts/{contact.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(
            new SiteContactResponse(contact.Id, siteId, "Г-н Петров", "собственик", null, "звъни след 18"),
            contact);
    }

    [Fact]
    public async Task List_OrderedByName_AndOnlyForThatSite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        await CreateContactAsync(owner, bath, Contact("Петров"));
        await CreateContactAsync(owner, kitchen, Contact("Георгиев"));
        await CreateContactAsync(owner, bath, Contact("Иванова", "съсед", "0888"));

        var contacts = await owner.GetFromJsonAsync<SiteContactResponse[]>($"/api/sites/{bath}/contacts");

        Assert.NotNull(contacts);
        Assert.Equal(["Иванова", "Петров"], contacts.Select(c => c.Name));
        Assert.Equal(("съсед", "0888"), (contacts[0].Role, contacts[0].Phone));
        Assert.All(contacts, c => Assert.Equal(bath, c.SiteId));
    }

    [Fact]
    public async Task Owner_Updates_ReplacesAllFields()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var contact = await CreateContactAsync(owner, siteId, Contact("Петров", "собственик", "0888", "бележка"));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/contacts/{contact.Id}",
            new SaveSiteContactRequest(" Петров ", null, " 0899 ", ""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new SiteContactResponse(contact.Id, siteId, "Петров", null, "0899", null),
            await response.Content.ReadFromJsonAsync<SiteContactResponse>());
    }

    [Fact]
    public async Task Owner_Deletes_Returns204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var contact = await CreateContactAsync(owner, siteId, Contact("Петров"));

        var response = await owner.DeleteAsync($"/api/sites/{siteId}/contacts/{contact.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<SiteContactResponse[]>($"/api/sites/{siteId}/contacts"))!);
    }

    [Fact]
    public async Task EmptyName_Returns400OnName()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync($"/api/sites/{siteId}/contacts", Contact("   ", new string('а', 51)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["name", "role"], problem?.Errors.Keys.Order());
    }

    [Fact]
    public async Task UnknownSite_Returns404_OnListAndCreate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = Guid.NewGuid();

        await AssertNotFound(await owner.GetAsync($"/api/sites/{siteId}/contacts"), SiteNotFoundTitle);
        await AssertNotFound(
            await owner.PostAsJsonAsync($"/api/sites/{siteId}/contacts", Contact("Петров")),
            SiteNotFoundTitle);
    }

    [Fact]
    public async Task UnknownContact_OrOfAnotherSite_Returns404_OnUpdateAndDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var kitchenContact = await CreateContactAsync(owner, kitchen, Contact("Георгиев"));

        await AssertNotFound(
            await owner.PutAsJsonAsync($"/api/sites/{bath}/contacts/{Guid.NewGuid()}", Contact("Петров")),
            ContactNotFoundTitle);
        await AssertNotFound(
            await owner.PutAsJsonAsync($"/api/sites/{bath}/contacts/{kitchenContact.Id}", Contact("Петров")),
            ContactNotFoundTitle);
        await AssertNotFound(await owner.DeleteAsync($"/api/sites/{bath}/contacts/{Guid.NewGuid()}"), ContactNotFoundTitle);
        await AssertNotFound(await owner.DeleteAsync($"/api/sites/{bath}/contacts/{kitchenContact.Id}"), ContactNotFoundTitle);

        var kept = await owner.GetFromJsonAsync<SiteContactResponse[]>($"/api/sites/{kitchen}/contacts");
        Assert.Equal(["Георгиев"], kept!.Select(c => c.Name));
    }

    [Fact]
    public async Task Manager_CreatesReadsUpdates_ButCannotDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await factory.CreateUserAsync("manager", Password, Roles.Manager);
        var manager = await factory.CreateLoggedInClientAsync("manager", Password);

        var create = await manager.PostAsJsonAsync($"/api/sites/{siteId}/contacts", Contact("Петров"));
        var contact = await create.Content.ReadFromJsonAsync<SiteContactResponse>();
        var list = await manager.GetAsync($"/api/sites/{siteId}/contacts");
        var update = await manager.PutAsJsonAsync($"/api/sites/{siteId}/contacts/{contact!.Id}", Contact("Петрови"));
        var delete = await manager.DeleteAsync($"/api/sites/{siteId}/contacts/{contact.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        var kept = await owner.GetFromJsonAsync<SiteContactResponse[]>($"/api/sites/{siteId}/contacts");
        Assert.Equal(["Петрови"], kept!.Select(c => c.Name));
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var contact = await CreateContactAsync(owner, siteId, Contact("Петров"));
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync($"/api/sites/{siteId}/contacts");
        var create = await client.PostAsJsonAsync($"/api/sites/{siteId}/contacts", Contact("Георгиев"));
        var update = await client.PutAsJsonAsync($"/api/sites/{siteId}/contacts/{contact.Id}", Contact("Георгиев"));
        var delete = await client.DeleteAsync($"/api/sites/{siteId}/contacts/{contact.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    // Contacts are not history: they do not block deleting the site and go with it.
    [Fact]
    public async Task DeletingTheSite_WithContacts_Returns204_AndTheContactsAreGone()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        await CreateContactAsync(owner, siteId, Contact("Петров"));
        await CreateContactAsync(owner, siteId, Contact("Иванова"));

        var response = await owner.DeleteAsync($"/api/sites/{siteId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await factory.CountSiteContactsAsync(siteId));
        await AssertNotFound(await owner.GetAsync($"/api/sites/{siteId}/contacts"), SiteNotFoundTitle);
    }

    private static SaveSiteContactRequest Contact(string name, string? role = null, string? phone = null, string? note = null)
        => new(name, role, phone, note);

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task<SiteContactResponse> CreateContactAsync(
        HttpClient client,
        Guid siteId,
        SaveSiteContactRequest request)
    {
        var response = await client.PostAsJsonAsync($"/api/sites/{siteId}/contacts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteContactResponse>())!;
    }

    private static async Task AssertNotFound(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
    }
}
