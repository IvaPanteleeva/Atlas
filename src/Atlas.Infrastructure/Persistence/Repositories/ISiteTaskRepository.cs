namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Tasks;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<SiteTask> needs the SiteTask type.
public interface ISiteTaskRepository : IRepository<SiteTask>
{
    // All tasks, or only those of a site, of a person, or of the company (scope "company": without a site).
    // Open first (oldest first), then the ticked ones (last ticked first). Validation on scope for an unknown scope.
    Task<Result<IReadOnlyList<TaskResponse>>> ListAsync(
        Guid? siteId,
        Guid? assigneeId,
        string? scope,
        CancellationToken cancellationToken);

    // Validation on siteId (unknown site) or assigneeId (unknown person, not crew).
    Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    // Replaces the text and the person responsible. NotFound; Validation on assigneeId (unknown person, or a new choice
    // who is not crew; the current one stays even when no longer crew).
    Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken);

    // Ticks the task off (DoneAt = now) or takes the tick away (DoneAt cleared). NotFound.
    Task<Result<TaskResponse>> SetDoneAsync(Guid id, SetTaskDoneRequest request, CancellationToken cancellationToken);

    // NotFound.
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
