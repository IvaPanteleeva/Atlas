namespace Atlas.Application.Health;

/// <summary>API and database health status.</summary>
/// <param name="Status">"ok" when everything works, "degraded" otherwise.</param>
/// <param name="Database">"ok" when the database answers, "unreachable" otherwise.</param>
public sealed record HealthResponse(string Status, string Database);
