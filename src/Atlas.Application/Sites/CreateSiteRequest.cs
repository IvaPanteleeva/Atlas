namespace Atlas.Application.Sites;

/// <summary>Payload for adding a site (Owner or Crew). Text is trimmed; empty optional text is stored as null.</summary>
/// <param name="Name">Required, up to 200 characters; may repeat.</param>
/// <param name="Status">Lead, Active or Done; Lead when missing.</param>
/// <param name="CompanyId">An existing company, or null for a direct customer.</param>
/// <param name="EndCustomer">Up to 200 characters.</param>
/// <param name="Address">Up to 300 characters.</param>
/// <param name="MapUrl">Absolute http:// or https:// link, up to 500 characters.</param>
/// <param name="Note">Internal note, up to 2000 characters.</param>
public sealed record CreateSiteRequest(
    string Name,
    SiteStatus? Status = null,
    Guid? CompanyId = null,
    string? EndCustomer = null,
    string? Address = null,
    string? MapUrl = null,
    string? Note = null);
