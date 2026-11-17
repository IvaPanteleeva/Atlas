namespace Atlas.Application.Auth;

/// <summary>The signed-in user.</summary>
/// <param name="Id">User id.</param>
/// <param name="UserName">User name.</param>
/// <param name="Role">One of Owner, Manager, ClientViewer.</param>
/// <param name="CompanyId">Company of a ClientViewer; null for Owner and Manager.</param>
/// <param name="PersonId">The person (from „Хора“) linked to this profile, or null. Used for the „Мои“ filter of the tasks.</param>
public sealed record MeResponse(Guid Id, string UserName, string Role, Guid? CompanyId, Guid? PersonId);
