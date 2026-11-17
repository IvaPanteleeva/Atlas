namespace Atlas.Application.Portal;

/// <summary>The client's sites and „Дължите“ (GET /portal/sites). Only the sites of the user's own company.</summary>
/// <param name="Sites">The company's sites at every status, in the order of the internal site list.</param>
/// <param name="Totals">Sums over these sites only.</param>
public sealed record PortalSitesResponse(
    IReadOnlyList<PortalSiteListResponse> Sites,
    PortalTotalsResponse Totals);
