namespace Atlas.Application.Portal;

using Atlas.Application.Sites;

/// <summary>One site in the client's list: no note, no company, no worker sums.</summary>
/// <param name="Id">The site.</param>
/// <param name="Name">Site name.</param>
/// <param name="EndCustomer">End customer (free text), or null.</param>
/// <param name="Status">Lead, Active or Done.</param>
/// <param name="Earned">„Изработено“ in EUR, the same number as inside.</param>
/// <param name="Paid">„Платено“ in EUR.</param>
/// <param name="Remaining">Earned − Paid; negative means overpaid.</param>
/// <param name="OpenCount">Items without a price or flagged („за уточняване“).</param>
public sealed record PortalSiteListResponse(
    Guid Id,
    string Name,
    string? EndCustomer,
    SiteStatus Status,
    decimal Earned,
    decimal Paid,
    decimal Remaining,
    int OpenCount);
