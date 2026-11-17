namespace Atlas.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Atlas.Application.Common;
using Atlas.Application.Tasks;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class SiteTaskRepository(AtlasDbContext data, TimeProvider timeProvider)
    : DataRepository<SiteTask>(data), ISiteTaskRepository
{
    private static readonly Expression<Func<SiteTask, TaskResponse>> ToResponse = t => new TaskResponse(
        t.Id,
        t.Text,
        t.Done,
        t.SiteId,
        t.AssigneeId,
        t.Assignee == null ? null : t.Assignee.Name,
        t.CreatedAt,
        t.DoneAt);

    public async Task<Result<IReadOnlyList<TaskResponse>>> ListAsync(
        Guid? siteId,
        Guid? assigneeId,
        string? scope,
        CancellationToken cancellationToken)
    {
        var companyOnly = scope switch
        {
            null or "" => false,
            TaskRules.CompanyScope => true,
            _ => (bool?)null,
        };
        if (companyOnly is null)
        {
            return Field("scope", TaskRules.UnknownScopeMessage);
        }

        var query = this.Data.SiteTasks.AsNoTracking();
        if (siteId is { } site)
        {
            query = query.Where(t => t.SiteId == site);
        }

        if (assigneeId is { } assignee)
        {
            query = query.Where(t => t.AssigneeId == assignee);
        }

        if (companyOnly.Value)
        {
            query = query.Where(t => t.SiteId == null);
        }

        // CLAUDE.md "Задачи": the open ones first, the oldest first; then the ticked ones, the last ticked first.
        // Within each group the other key is the same (null or ignored); Id only makes ties stable.
        var tasks = await query
            .OrderBy(t => t.Done)
            .ThenBy(t => t.Done ? null : (DateTimeOffset?)t.CreatedAt)
            .ThenByDescending(t => t.DoneAt)
            .ThenBy(t => t.Id)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TaskResponse>>.SuccessWith(tasks);
    }

    public async Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.SiteId is { } siteId && !await this.Data.Sites.AnyAsync(s => s.Id == siteId, cancellationToken))
        {
            return Field("siteId", SiteRepository.NotFoundMessage);
        }

        if (await this.AssigneeErrorAsync(null, request.AssigneeId, cancellationToken) is { } error)
        {
            return error;
        }

        var task = new SiteTask
        {
            Text = TaskRules.NormalizeText(request.Text),
            SiteId = request.SiteId,
            AssigneeId = request.AssigneeId,
            CreatedAt = this.Now(),
        };
        await this.AddAsync(task, cancellationToken);

        return await this.SaveAndGetAsync(task, cancellationToken);
    }

    public async Task<Result<TaskResponse>> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await this.Data.SiteTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return Error.NotFound(TaskRules.NotFoundMessage);
        }

        if (await this.AssigneeErrorAsync(task.AssigneeId, request.AssigneeId, cancellationToken) is { } error)
        {
            return error;
        }

        task.Text = TaskRules.NormalizeText(request.Text);
        task.AssigneeId = request.AssigneeId;

        return await this.SaveAndGetAsync(task, cancellationToken);
    }

    public async Task<Result<TaskResponse>> SetDoneAsync(
        Guid id,
        SetTaskDoneRequest request,
        CancellationToken cancellationToken)
    {
        var task = await this.Data.SiteTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return Error.NotFound(TaskRules.NotFoundMessage);
        }

        // The validator makes Done required; the fallback only keeps the compiler happy.
        if (request.Done ?? task.Done)
        {
            task.Complete(this.Now());
        }
        else
        {
            task.Reopen();
        }

        return await this.SaveAndGetAsync(task, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await this.Data.SiteTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return Error.NotFound(TaskRules.NotFoundMessage);
        }

        this.Delete(task);
        await this.SaveAsync(cancellationToken);

        return Result.Success;
    }

    private static Error Field(string field, string message)
        => Error.Validation(new Dictionary<string, string[]> { [field] = [message] });

    // Postgres keeps microseconds; cutting the rest off here makes the answer the same as what is read back later.
    private DateTimeOffset Now()
    {
        var now = timeProvider.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    // Null when nobody is chosen or the choice is allowed (TaskRules.AssigneeError); otherwise a 400 on assigneeId.
    private async Task<Error?> AssigneeErrorAsync(Guid? current, Guid? chosen, CancellationToken cancellationToken)
    {
        if (chosen is not { } personId)
        {
            return null;
        }

        var isCrew = await this.Data.People
            .Where(p => p.Id == personId)
            .Select(p => (bool?)p.IsCrew)
            .FirstOrDefaultAsync(cancellationToken);

        return TaskRules.AssigneeError(current == personId, isCrew) is { } message
            ? Field("assigneeId", message)
            : null;
    }

    // Reads the task back so the answer has the name of the person responsible.
    private async Task<Result<TaskResponse>> SaveAndGetAsync(SiteTask task, CancellationToken cancellationToken)
    {
        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        } postgres)
        {
            // The site or the person was deleted between the check and the save
            // (FK_SiteTasks_Sites_SiteId or FK_SiteTasks_People_AssigneeId).
            return postgres.ConstraintName?.EndsWith("_SiteId", StringComparison.Ordinal) == true
                ? Field("siteId", SiteRepository.NotFoundMessage)
                : Field("assigneeId", TaskRules.AssigneeNotFoundMessage);
        }

        return await this.Data.SiteTasks
            .AsNoTracking()
            .Where(t => t.Id == task.Id)
            .Select(ToResponse)
            .FirstAsync(cancellationToken);
    }
}
