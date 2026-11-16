namespace Atlas.Application.Users;

/// <summary>A user as seen by the Owner.</summary>
/// <param name="Id">User id.</param>
/// <param name="UserName">User name.</param>
/// <param name="Role">One of Owner, Crew, ClientViewer.</param>
/// <param name="CompanyId">Company of a ClientViewer; null for Owner and Crew.</param>
/// <param name="CompanyName">Name of that company.</param>
public sealed record UserResponse(Guid Id, string UserName, string Role, Guid? CompanyId, string? CompanyName);
