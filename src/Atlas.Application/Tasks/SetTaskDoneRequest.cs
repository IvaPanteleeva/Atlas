namespace Atlas.Application.Tasks;

/// <summary>Payload for ticking a task off (true) or taking the tick away (false).</summary>
/// <param name="Done">Required.</param>
public sealed record SetTaskDoneRequest(bool? Done);
