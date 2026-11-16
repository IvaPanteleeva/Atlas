using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Atlas.Application.Common;
using Atlas.Application.Identity;
using Atlas.Application.Payments;
using Atlas.Application.Sites;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.IntegrationTests;

// Client payments of one site (CLAUDE.md "Плащания и суми"): only the Owner sees and changes them.
[Collection("Api")]
public class PaymentEndpointsTests(AtlasApiFactory factory)
{
    private const string Password = "crew-password";

    private const string SiteNotFoundTitle = "Няма такъв обект.";

    private const string PaymentNotFoundTitle = "Няма такова плащане.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Same rule as the server: the date in Bulgaria.
    private static DateOnly Today => WorkDayRules.Today(TimeProvider.System);

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var client = factory.CreateClient();
        var siteId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/sites/{siteId}/payments");
        var create = await client.PostAsJsonAsync($"/api/sites/{siteId}/payments", Payment(10m), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Owner_Creates_Returns201WithLocation_AndNormalizedNote()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var date = Today.AddDays(-3);

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/payments",
            new SavePaymentRequest(date, 1157.89m, "  аванс, в брой  "),
            Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(Json);
        Assert.NotNull(payment);
        Assert.Equal($"/api/sites/{siteId}/payments/{payment.Id}", response.Headers.Location?.OriginalString);
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(new PaymentResponse(payment.Id, siteId, date, 1157.89m, "аванс, в брой"), payment);

        var list = await owner.GetFromJsonAsync<PaymentResponse[]>($"/api/sites/{siteId}/payments", Json);
        Assert.Equal([payment], list!);
    }

    [Fact]
    public async Task EmptyNote_IsStoredAsNull_AndDateTravelsAsIsoDate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsync(
            $"/api/sites/{siteId}/payments",
            JsonBody("""{ "date": "2026-03-05", "amount": 50.5, "note": "   " }"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal("2026-03-05", body["date"]!.GetValue<string>());
        Assert.Equal(50.5m, body["amount"]!.GetValue<decimal>());
        Assert.Null(body["note"]);
    }

    [Fact]
    public async Task List_NewestFirst_ByDateThenEntry_AndOnlyForThatSite()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        await CreatePaymentAsync(owner, bath, new SavePaymentRequest(Today.AddDays(-5), 10m, "Стар"));
        await CreatePaymentAsync(owner, bath, new SavePaymentRequest(Today, 20m, "Днес 1"));
        await CreatePaymentAsync(owner, bath, new SavePaymentRequest(Today.AddDays(-1), 30m, "Вчера"));
        await CreatePaymentAsync(owner, bath, new SavePaymentRequest(Today, 40m, "Днес 2"));
        await CreatePaymentAsync(owner, kitchen, new SavePaymentRequest(Today, 50m, "Кухня"));

        var payments = await owner.GetFromJsonAsync<PaymentResponse[]>($"/api/sites/{bath}/payments", Json);

        Assert.Equal(["Днес 2", "Днес 1", "Вчера", "Стар"], payments!.Select(p => p.Note));
    }

    [Fact]
    public async Task Owner_Updates_Returns200()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var payment = await CreatePaymentAsync(owner, siteId, new SavePaymentRequest(Today, 10m, "аванс"));

        var response = await owner.PutAsJsonAsync(
            $"/api/sites/{siteId}/payments/{payment.Id}",
            new SavePaymentRequest(Today.AddDays(-1), 25.5m, null),
            Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PaymentResponse>(Json);
        Assert.Equal(new PaymentResponse(payment.Id, siteId, Today.AddDays(-1), 25.5m, null), updated);

        var list = await owner.GetFromJsonAsync<PaymentResponse[]>($"/api/sites/{siteId}/payments", Json);
        Assert.Equal([updated!], list!);
    }

    [Fact]
    public async Task Owner_Deletes_Returns204_ThenItIsGone()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var payment = await CreatePaymentAsync(owner, siteId, Payment(10m));

        var response = await owner.DeleteAsync($"/api/sites/{siteId}/payments/{payment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<PaymentResponse[]>($"/api/sites/{siteId}/payments", Json))!);
        await AssertNotFound(await owner.DeleteAsync($"/api/sites/{siteId}/payments/{payment.Id}"), PaymentNotFoundTitle);
    }

    [Fact]
    public async Task UnknownSite_Returns404_OnListAndCreate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = Guid.NewGuid();

        await AssertNotFound(await owner.GetAsync($"/api/sites/{siteId}/payments"), SiteNotFoundTitle);
        await AssertNotFound(
            await owner.PostAsJsonAsync($"/api/sites/{siteId}/payments", Payment(10m), Json),
            SiteNotFoundTitle);
    }

    [Fact]
    public async Task UnknownPayment_Returns404_OnPutAndDelete()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var id = Guid.NewGuid();

        await AssertNotFound(
            await owner.PutAsJsonAsync($"/api/sites/{siteId}/payments/{id}", Payment(10m), Json),
            PaymentNotFoundTitle);
        await AssertNotFound(await owner.DeleteAsync($"/api/sites/{siteId}/payments/{id}"), PaymentNotFoundTitle);
    }

    [Fact]
    public async Task PaymentOfAnotherSite_Returns404_AndStaysUnchanged()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var bath = await CreateSiteAsync(owner, "Баня");
        var kitchen = await CreateSiteAsync(owner, "Кухня");
        var payment = await CreatePaymentAsync(owner, bath, Payment(10m));

        await AssertNotFound(
            await owner.PutAsJsonAsync($"/api/sites/{kitchen}/payments/{payment.Id}", Payment(99m), Json),
            PaymentNotFoundTitle);
        await AssertNotFound(
            await owner.DeleteAsync($"/api/sites/{kitchen}/payments/{payment.Id}"),
            PaymentNotFoundTitle);

        var list = await owner.GetFromJsonAsync<PaymentResponse[]>($"/api/sites/{bath}/payments", Json);
        Assert.Equal([payment], list!);
    }

    [Theory]
    [InlineData("0", PaymentRules.MustBePositiveMessage)]
    [InlineData("1000000.01", PaymentRules.TooLargeMessage)]
    [InlineData("10.005", PaymentRules.TooManyDecimalsMessage)]
    public async Task BadAmount_Returns400OnAmount(string amount, string message)
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsync(
            $"/api/sites/{siteId}/payments",
            JsonBody($$"""{ "date": "{{Today:yyyy-MM-dd}}", "amount": {{amount}} }"""));

        var problem = await AssertFieldError(response, "amount");
        Assert.Equal([message], problem.Errors["amount"]);
    }

    [Fact]
    public async Task MaxAmount_IsAccepted()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var payment = await CreatePaymentAsync(owner, siteId, Payment(1_000_000m));

        Assert.Equal(1_000_000m, payment.Amount);
    }

    [Fact]
    public async Task MissingAmount_Returns400OnAmount_MissingDate_Returns400OnDate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        await AssertFieldError(
            await owner.PostAsync($"/api/sites/{siteId}/payments", JsonBody($$"""{ "date": "{{Today:yyyy-MM-dd}}" }""")),
            "amount");
        await AssertFieldError(
            await owner.PostAsync($"/api/sites/{siteId}/payments", JsonBody("""{ "amount": 10 }""")),
            "date");
    }

    [Fact]
    public async Task FutureDate_Returns400OnDate_OnCreateAndUpdate()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var payment = await CreatePaymentAsync(owner, siteId, Payment(10m));
        var future = new SavePaymentRequest(Today.AddDays(1), 10m, null);

        var create = await AssertFieldError(
            await owner.PostAsJsonAsync($"/api/sites/{siteId}/payments", future, Json),
            "date");
        var update = await AssertFieldError(
            await owner.PutAsJsonAsync($"/api/sites/{siteId}/payments/{payment.Id}", future, Json),
            "date");

        Assert.Equal([WorkDayRules.FutureDateMessage], create.Errors["date"]);
        Assert.Equal([WorkDayRules.FutureDateMessage], update.Errors["date"]);
    }

    [Fact]
    public async Task Note201_Returns400OnNote()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");

        var response = await owner.PostAsJsonAsync(
            $"/api/sites/{siteId}/payments",
            new SavePaymentRequest(Today, 10m, new string('а', 201)),
            Json);

        await AssertFieldError(response, "note");
    }

    [Fact]
    public async Task Crew_Gets403_OnEverything()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var payment = await CreatePaymentAsync(owner, siteId, Payment(10m));
        await factory.CreateUserAsync("crew", Password, Roles.Crew);
        var crew = await factory.CreateLoggedInClientAsync("crew", Password);

        var list = await crew.GetAsync($"/api/sites/{siteId}/payments");
        var create = await crew.PostAsJsonAsync($"/api/sites/{siteId}/payments", Payment(20m), Json);
        var update = await crew.PutAsJsonAsync($"/api/sites/{siteId}/payments/{payment.Id}", Payment(20m), Json);
        var delete = await crew.DeleteAsync($"/api/sites/{siteId}/payments/{payment.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        var stored = await owner.GetFromJsonAsync<PaymentResponse[]>($"/api/sites/{siteId}/payments", Json);
        Assert.Equal([payment], stored!);
    }

    [Fact]
    public async Task ClientViewer_Gets403()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var payment = await CreatePaymentAsync(owner, siteId, Payment(10m));
        await factory.CreateUserAsync("zhenistroy", Password, Roles.ClientViewer, CompanyConfiguration.ZhenistroyId);
        var client = await factory.CreateLoggedInClientAsync("zhenistroy", Password);

        var list = await client.GetAsync($"/api/sites/{siteId}/payments");
        var create = await client.PostAsJsonAsync($"/api/sites/{siteId}/payments", Payment(20m), Json);
        var delete = await client.DeleteAsync($"/api/sites/{siteId}/payments/{payment.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteSite_WithAPayment_Returns409_ThenWithoutPayments_204()
    {
        await factory.ResetDatabaseAsync();
        var owner = await factory.CreateOwnerClientAsync();
        var siteId = await CreateSiteAsync(owner, "Баня");
        var payment = await CreatePaymentAsync(owner, siteId, Payment(10m));

        var conflict = await owner.DeleteAsync($"/api/sites/{siteId}");

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var problem = await conflict.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Обектът има записи", problem?.Title);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/sites/{siteId}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await owner.DeleteAsync($"/api/sites/{siteId}/payments/{payment.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/sites/{siteId}")).StatusCode);
    }

    private static SavePaymentRequest Payment(decimal amount)
        => new(Today, amount, null);

    private static StringContent JsonBody(string json)
        => new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task<PaymentResponse> CreatePaymentAsync(
        HttpClient owner,
        Guid siteId,
        SavePaymentRequest request)
    {
        var response = await owner.PostAsJsonAsync($"/api/sites/{siteId}/payments", request, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(Json);
        Assert.NotNull(payment);
        return payment;
    }

    private static async Task<Guid> CreateSiteAsync(HttpClient owner, string name, SiteStatus? status = null)
    {
        var response = await owner.PostAsJsonAsync("/api/sites", new CreateSiteRequest(name, status), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SiteResponse>(Json))!.Id;
    }

    private static async Task AssertNotFound(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem?.Title);
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
