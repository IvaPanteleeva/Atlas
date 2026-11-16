namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Persistence.Repositories;

// Work days of one site. Owner and Manager add, read and edit them, prices included; only the Owner deletes
// (CLAUDE.md "Работни дни"). The prices are stored as sent.
public static class WorkDayEndpoints
{
    public static IEndpointRouteBuilder MapWorkDayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites/{siteId:guid}/workdays")
            .WithTags("WorkDays")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetWorkDays)
            .WithName("GetWorkDays")
            .WithSummary("List the work days of a site")
            .WithDescription("The days with their items, sums and open count; newest first (date, then entry time).")
            .Produces<IReadOnlyList<WorkDayResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetWorkDay)
            .WithName("GetWorkDay")
            .WithSummary("Get a work day")
            .WithDescription("One day of the site with its items, sum and open count.")
            .Produces<WorkDayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateWorkDay)
            .WithValidation<SaveWorkDayRequest>()
            .WithName("CreateWorkDay")
            .WithSummary("Add a work day")
            .WithDescription(
                "Adds a day with 1–50 items; the prices are stored as sent (null = „цена?“). "
                + "A Lead site becomes Active.")
            .Produces<WorkDayResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateWorkDay)
            .WithValidation<SaveWorkDayRequest>()
            .WithName("UpdateWorkDay")
            .WithSummary("Edit a work day")
            .WithDescription(
                "Replaces the day and its items (kept items by id); the prices are stored as sent. "
                + "WorkerIds null keeps the workers.")
            .Produces<WorkDayResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteWorkDay)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteWorkDay")
            .WithSummary("Delete a work day")
            .WithDescription("Deletes the day with its items (Owner only).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetWorkDays(
        Guid siteId,
        IWorkDayRepository workDays,
        CancellationToken cancellationToken)
        => (await workDays.ListAsync(siteId, cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetWorkDay(
        Guid siteId,
        Guid id,
        IWorkDayRepository workDays,
        CancellationToken cancellationToken)
        => (await workDays.GetAsync(siteId, id, cancellationToken)).ToHttpResult();

    private static async Task<IResult> CreateWorkDay(
        Guid siteId,
        SaveWorkDayRequest request,
        IWorkDayRepository workDays,
        CancellationToken cancellationToken)
        => (await workDays.CreateAsync(siteId, request, cancellationToken))
            .ToHttpResult(day => TypedResults.Created($"/api/sites/{siteId}/workdays/{day.Id}", day));

    private static async Task<IResult> UpdateWorkDay(
        Guid siteId,
        Guid id,
        SaveWorkDayRequest request,
        IWorkDayRepository workDays,
        CancellationToken cancellationToken)
        => (await workDays.UpdateAsync(siteId, id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteWorkDay(
        Guid siteId,
        Guid id,
        IWorkDayRepository workDays,
        CancellationToken cancellationToken)
        => (await workDays.DeleteAsync(siteId, id, cancellationToken)).ToHttpResult();
}
