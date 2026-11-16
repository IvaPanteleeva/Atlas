namespace Atlas.Application.Sites;

/// <summary>
/// A site in the list: Active first (latest work day first, sites without days last), then Lead, then Done
/// (both by name). No note and no money (those come from the site and the summaries).
/// </summary>
/// <param name="Id">Site id.</param>
/// <param name="Name">Site name.</param>
/// <param name="Status">Lead, Active or Done.</param>
/// <param name="CompanyName">Company name, if the site has a company.</param>
/// <param name="EndCustomer">End customer, free text.</param>
/// <param name="Address">Address.</param>
/// <param name="LastWorkDate">Date of the latest work day ("yyyy-MM-dd"), or null without days.</param>
public sealed record SiteListResponse(
    Guid Id,
    string Name,
    SiteStatus Status,
    string? CompanyName,
    string? EndCustomer,
    string? Address,
    DateOnly? LastWorkDate);
