using System.Text.Json.Serialization;
using Atlas.Api.Configuration;
using Atlas.Api.Endpoints;
using Atlas.Application;
using Atlas.Infrastructure.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Enums travel as strings ("Lead", not 0), in the JSON and in the OpenAPI document.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddAtlasAuth();

var app = builder.Build();

await app.InitializeDatabaseAsync();

// Errors are ProblemDetails. In Development the built-in developer exception page
// (added by WebApplication) answers with ProblemDetails that include the exception;
// UseExceptionHandler would catch first and hide it, so it runs only outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// Empty 4xx/5xx responses get a ProblemDetails body (also the 401/403 from the cookie and the policies).
app.UseStatusCodePages();

// Open in Development: they only describe the API, every call still needs a login.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseHttpsRedirection();

// Angular build output lives in wwwroot (see CLAUDE.md "Деплой").
// Before authentication: the Angular files are public, the login screen is one of them.
app.UseStaticFiles();

// Every endpoint below needs a login unless it says AllowAnonymous (fallback policy in AddAtlasAuth).
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapCompanyEndpoints();
app.MapSiteEndpoints();
app.MapServiceEndpoints();
app.MapSitePriceEndpoints();

// Unknown /api/... routes are 404 ProblemDetails, not index.html.
// The literal "api" segment makes this more specific than the Angular fallback below.
// Anonymous, so a wrong address is a 404 and not a 401.
app.MapGroup("/api")
    .MapFallback(() => TypedResults.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Няма такъв адрес в API-то"))
    .AllowAnonymous();

// Client-side routes (e.g. /sites/5) get index.html so Angular routing can handle them.
// Anonymous: Angular itself shows the login screen and asks /api/auth/me.
app.MapFallbackToFile("index.html").AllowAnonymous();

await app.RunAsync();
