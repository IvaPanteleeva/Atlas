namespace Atlas.Application.Users;

/// <summary>A user as seen by the Owner.</summary>
/// <param name="Id">User id.</param>
/// <param name="UserName">User name.</param>
/// <param name="Role">One of Owner, Manager, ClientViewer.</param>
/// <param name="CompanyId">Company of a ClientViewer; null for Owner and Manager.</param>
/// <param name="CompanyName">Name of that company.</param>
/// <param name="PersonId">The crew member linked to this profile, or null.</param>
/// <param name="PersonName">That person's name.</param>
public sealed record UserResponse(
    Guid Id,
    string UserName,
    string Role,
    Guid? CompanyId,
    string? CompanyName,
    Guid? PersonId,
    string? PersonName);
