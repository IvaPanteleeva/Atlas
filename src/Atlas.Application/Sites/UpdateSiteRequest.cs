namespace Atlas.Application.Sites;

/// <summary>Payload for editing a site (Owner or Crew). Replaces all fields; same rules as when adding.</summary>
/// <param name="Name">Required, up to 200 characters; may repeat.</param>
/// <param name="Status">Required: Lead, Active or Done (nullable only so a missing value is a 400, not Lead).</param>
/// <param name="CompanyId">An existing company, or null for a direct customer.</param>
/// <param name="EndCustomer">Up to 200 characters.</param>
/// <param name="Address">Up to 300 characters.</param>
/// <param name="MapUrl">Absolute http:// or https:// link, up to 500 characters.</param>
/// <param name="Note">Internal note, up to 2000 characters.</param>
public sealed record UpdateSiteRequest(
    string Name,
    SiteStatus? Status,
    Guid? CompanyId = null,
    string? EndCustomer = null,
    string? Address = null,
    string? MapUrl = null,
    string? Note = null);
