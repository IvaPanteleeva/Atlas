namespace Atlas.Application.Auth;

/// <summary>User name and password for signing in.</summary>
/// <param name="UserName">User name (not e-mail).</param>
/// <param name="Password">Password.</param>
public sealed record LoginRequest(string UserName, string Password);
