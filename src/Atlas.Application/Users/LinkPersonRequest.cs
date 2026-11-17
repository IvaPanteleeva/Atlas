namespace Atlas.Application.Users;

/// <summary>Payload for linking a login profile to a crew member, or unlinking it (Owner only).</summary>
/// <param name="PersonId">The person (must be crew and not linked to another profile), or null to unlink.</param>
public sealed record LinkPersonRequest(Guid? PersonId);
