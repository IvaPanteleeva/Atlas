using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Angular build output lives in wwwroot (see CLAUDE.md "Деплой").
app.UseStaticFiles();

// Client-side routes (e.g. /sites/5) get index.html so Angular routing can handle them.
app.MapFallbackToFile("index.html");

app.Run();
