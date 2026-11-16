namespace Atlas.Application.Summaries;

/// <summary>The sums of every site (any status) and their totals, for the site list.</summary>
/// <param name="Totals">Sums over all sites.</param>
/// <param name="Sites">One entry per site, matched by SiteId (ordered by SiteId, not by the list order).</param>
public sealed record SiteSummariesResponse(
    SummaryTotalsResponse Totals,
    IReadOnlyList<SiteSummaryListResponse> Sites);
