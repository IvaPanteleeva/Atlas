namespace Atlas.Application.Tasks;

/// <summary>
/// Payload for adding a task (Owner or Manager). Without a site the task belongs to the company.
/// The text is trimmed.
/// </summary>
/// <param name="Text">Required, up to 500 characters.</param>
/// <param name="SiteId">The site, or null for a task of the company. Cannot be changed later.</param>
/// <param name="AssigneeId">The person responsible, optional; must be one of the crew.</param>
public sealed record CreateTaskRequest(string Text, Guid? SiteId = null, Guid? AssigneeId = null);
