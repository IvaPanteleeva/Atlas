namespace Atlas.Application.Users;

/// <summary>Payload for creating a user (Owner only).</summary>
/// <param name="UserName">User name for signing in: Latin or Cyrillic letters, digits and - . _ @.</param>
/// <param name="Password">At least 8 characters.</param>
/// <param name="Role">One of Owner, Manager, ClientViewer.</param>
/// <param name="CompanyId">Required for ClientViewer, must be empty for Owner and Manager.</param>
/// <param name="PersonId">Optional: the crew member this profile belongs to. An Owner or Manager linked to a person makes
/// that person a partner („съдружник“). Not allowed for ClientViewer.</param>
public sealed record CreateUserRequest(string UserName, string Password, string Role, Guid? CompanyId, Guid? PersonId = null);
