namespace Atlas.Application.Users;

/// <summary>Payload for creating a user (Owner only).</summary>
/// <param name="UserName">User name for signing in: Latin or Cyrillic letters, digits and - . _ @.</param>
/// <param name="Password">At least 8 characters.</param>
/// <param name="Role">One of Owner, Crew, ClientViewer.</param>
/// <param name="CompanyId">Required for ClientViewer, must be empty for Owner and Crew.</param>
public sealed record CreateUserRequest(string UserName, string Password, string Role, Guid? CompanyId);
