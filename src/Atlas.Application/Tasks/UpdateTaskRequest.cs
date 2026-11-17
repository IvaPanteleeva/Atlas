namespace Atlas.Application.Tasks;

/// <summary>
/// Payload for editing a task (Owner or Manager): the text and the person responsible are replaced.
/// The text is trimmed.
/// </summary>
/// <param name="Text">Required, up to 500 characters.</param>
/// <param name="AssigneeId">The person responsible, or null for nobody. A new choice must be one of the crew;
/// the current one may stay even when no longer crew.</param>
public sealed record UpdateTaskRequest(string Text, Guid? AssigneeId = null);
