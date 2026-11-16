namespace Atlas.Application.Users;

/// <summary>Payload for setting a new password of a user (Owner only). The user's other sessions stop working.</summary>
/// <param name="Password">At least 8 characters.</param>
public sealed record SetPasswordRequest(string Password);
