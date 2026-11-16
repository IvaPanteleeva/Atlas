namespace Atlas.Application.Auth;

/// <summary>The signed-in user.</summary>
/// <param name="Id">User id.</param>
/// <param name="UserName">User name.</param>
/// <param name="Role">One of Owner, Crew, ClientViewer.</param>
/// <param name="CompanyId">Company of a ClientViewer; null for Owner and Crew.</param>
public sealed record MeResponse(Guid Id, string UserName, string Role, Guid? CompanyId);
