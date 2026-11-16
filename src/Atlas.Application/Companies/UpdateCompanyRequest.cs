namespace Atlas.Application.Companies;

/// <summary>Payload for renaming a company (Owner only).</summary>
/// <param name="Name">New unique name, up to 200 characters; leading and trailing spaces are removed.</param>
public sealed record UpdateCompanyRequest(string Name);
