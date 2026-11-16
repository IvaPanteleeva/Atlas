namespace Atlas.Application.Companies;

/// <summary>Payload for adding a company (Owner only).</summary>
/// <param name="Name">Unique name, up to 200 characters; leading and trailing spaces are removed.</param>
public sealed record CreateCompanyRequest(string Name);
