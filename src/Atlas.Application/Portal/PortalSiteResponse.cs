namespace Atlas.Application.Portal;

using Atlas.Application.Sites;

/// <summary>
/// One site of the client's company (GET /portal/sites/{id}): the site, its sums, days and payments.
/// No note, contacts, tasks, site prices or people.
/// </summary>
/// <param name="Id">The site.</param>
/// <param name="Name">Site name.</param>
/// <param name="EndCustomer">End customer (free text), or null.</param>
/// <param name="Address">Address, or null.</param>
/// <param name="MapUrl">http/https map link, or null.</param>
/// <param name="Status">Lead, Active or Done.</param>
/// <param name="Summary">The same sums as inside.</param>
/// <param name="WorkDays">Newest first.</param>
/// <param name="Payments">Newest first, like the internal list.</param>
public sealed record PortalSiteResponse(
    Guid Id,
    string Name,
    string? EndCustomer,
    string? Address,
    string? MapUrl,
    SiteStatus Status,
    PortalSummaryResponse Summary,
    IReadOnlyList<PortalWorkDayResponse> WorkDays,
    IReadOnlyList<PortalPaymentResponse> Payments);
