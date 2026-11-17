namespace Atlas.Application.Tasks;

/// <summary>A task of a site or of the company. Not shown in the client portal.</summary>
/// <param name="Id">Task id.</param>
/// <param name="Text">What has to be done.</param>
/// <param name="Done">Ticked off.</param>
/// <param name="SiteId">The site, or null for a task of the company.</param>
/// <param name="AssigneeId">The person responsible, or null.</param>
/// <param name="AssigneeName">Their name, or null.</param>
/// <param name="CreatedAt">When the task was added.</param>
/// <param name="DoneAt">When it was ticked off, or null while open.</param>
public sealed record TaskResponse(
    Guid Id,
    string Text,
    bool Done,
    Guid? SiteId,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DoneAt);
