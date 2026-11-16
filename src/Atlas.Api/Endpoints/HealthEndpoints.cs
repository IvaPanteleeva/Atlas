namespace Atlas.Api.Endpoints;

using Atlas.Application.Health;
using Atlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous: monitoring and the deploy check call it without a login.
        var group = app.MapGroup("/api/health")
            .WithTags("Health")
            .AllowAnonymous();

        group.MapGet("/", GetHealth)
            .WithName("GetHealth")
            .WithSummary("API and database health")
            .WithDescription("Returns 200 when the database answers, 503 when it does not.")
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<Results<Ok<HealthResponse>, JsonHttpResult<HealthResponse>>> GetHealth(
        AtlasDbContext db,
        CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? TypedResults.Ok(new HealthResponse("ok", "ok"))
            : TypedResults.Json(
                new HealthResponse("degraded", "unreachable"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
