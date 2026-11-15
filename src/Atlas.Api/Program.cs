using Atlas.Api.Configuration;
using Atlas.Api.Endpoints;
using Atlas.Application;
using Atlas.Infrastructure.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Angular build output lives in wwwroot (see CLAUDE.md "Деплой").
app.UseStaticFiles();

app.MapHealthEndpoints();

// Client-side routes (e.g. /sites/5) get index.html so Angular routing can handle them.
app.MapFallbackToFile("index.html");

await app.RunAsync();
