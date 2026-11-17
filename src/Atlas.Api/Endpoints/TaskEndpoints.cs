namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Tasks;
using Atlas.Infrastructure.Persistence.Repositories;

// Tasks of the sites and of the company (CLAUDE.md "Задачи"): Owner and Manager read, add, edit and tick them off;
// only the Owner deletes. The client company never sees them.
public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetTasks)
            .WithName("GetTasks")
            .WithSummary("List tasks")
            .WithDescription("All tasks, or only those of a site (siteId), of a person (assigneeId) or of the company "
                + "(scope=company: without a site). Open first, oldest first; then the ticked ones, last ticked first.")
            .Produces<IReadOnlyList<TaskResponse>>()
            .ProducesValidationProblem();

        group.MapPost("/", CreateTask)
            .WithValidation<CreateTaskRequest>()
            .WithName("CreateTask")
            .WithSummary("Add a task")
            .WithDescription("Adds a task to a site, or to the company without siteId. The person responsible must be "
                + "one of the crew.")
            .Produces<TaskResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateTask)
            .WithValidation<UpdateTaskRequest>()
            .WithName("UpdateTask")
            .WithSummary("Edit a task")
            .WithDescription("Replaces the text and the person responsible. A new person responsible must be one of the "
                + "crew; the current one may stay.")
            .Produces<TaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/done", SetTaskDone)
            .WithValidation<SetTaskDoneRequest>()
            .WithName("SetTaskDone")
            .WithSummary("Tick a task off or take the tick away")
            .WithDescription("done=true records the moment (doneAt); done=false clears it.")
            .Produces<TaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteTask)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteTask")
            .WithSummary("Delete a task")
            .WithDescription("Deletes the task (Owner only).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetTasks(
        Guid? siteId,
        Guid? assigneeId,
        string? scope,
        ISiteTaskRepository tasks,
        CancellationToken cancellationToken)
        => (await tasks.ListAsync(siteId, assigneeId, scope, cancellationToken)).ToHttpResult();

    private static async Task<IResult> CreateTask(
        CreateTaskRequest request,
        ISiteTaskRepository tasks,
        CancellationToken cancellationToken)
        => (await tasks.CreateAsync(request, cancellationToken))
            .ToHttpResult(task => TypedResults.Created($"/api/tasks/{task.Id}", task));

    private static async Task<IResult> UpdateTask(
        Guid id,
        UpdateTaskRequest request,
        ISiteTaskRepository tasks,
        CancellationToken cancellationToken)
        => (await tasks.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetTaskDone(
        Guid id,
        SetTaskDoneRequest request,
        ISiteTaskRepository tasks,
        CancellationToken cancellationToken)
        => (await tasks.SetDoneAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteTask(
        Guid id,
        ISiteTaskRepository tasks,
        CancellationToken cancellationToken)
        => (await tasks.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
