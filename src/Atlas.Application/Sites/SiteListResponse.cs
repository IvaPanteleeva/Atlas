namespace Atlas.Application.Sites;

/// <summary>A site in the list: Active first, then Lead, then Done; by name inside each group. No note.</summary>
/// <param name="Id">Site id.</param>
/// <param name="Name">Site name.</param>
/// <param name="Status">Lead, Active or Done.</param>
/// <param name="CompanyName">Company name, if the site has a company.</param>
/// <param name="EndCustomer">End customer, free text.</param>
/// <param name="Address">Address.</param>
public sealed record SiteListResponse(
    Guid Id,
    string Name,
    SiteStatus Status,
    string? CompanyName,
    string? EndCustomer,
    string? Address);
