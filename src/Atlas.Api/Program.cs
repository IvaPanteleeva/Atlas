using Atlas.Api.Configuration;
using Atlas.Api.Endpoints;
using Atlas.Application;
using Atlas.Infrastructure.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync();

// Errors are ProblemDetails. In Development the built-in developer exception page
// (added by WebApplication) answers with ProblemDetails that include the exception;
// UseExceptionHandler would catch first and hide it, so it runs only outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// Empty 4xx/5xx responses get a ProblemDetails body.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Angular build output lives in wwwroot (see CLAUDE.md "Деплой").
app.UseStaticFiles();

app.MapHealthEndpoints();

// Unknown /api/... routes are 404 ProblemDetails, not index.html.
// The literal "api" segment makes this more specific than the Angular fallback below.
app.MapGroup("/api").MapFallback(() => TypedResults.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Няма такъв адрес в API-то"));

// Client-side routes (e.g. /sites/5) get index.html so Angular routing can handle them.
app.MapFallbackToFile("index.html");

await app.RunAsync();
