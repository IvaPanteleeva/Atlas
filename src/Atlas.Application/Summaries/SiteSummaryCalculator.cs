namespace Atlas.Application.Summaries;

using Atlas.Application.WorkDays;

/// <summary>What the sums need from a work item: no description, no unit.</summary>
public readonly record struct SummaryItem(decimal Quantity, decimal? Rate, bool NeedsReview);

/// <summary>Earned, Paid, Remaining (Earned − Paid, negative = overpaid) and the open count.</summary>
public sealed record SiteSummary(decimal Earned, decimal Paid, decimal Remaining, int OpenCount);

/// <summary>The sums over all sites: Owed and Overpaid are kept apart, never netted.</summary>
public sealed record SummaryTotals(
    decimal Earned,
    decimal Paid,
    decimal Owed,
    decimal Overpaid,
    int OverpaidSiteCount,
    int OpenCount);

// The sums of CLAUDE.md "Плащания и суми" in one place, for the day (WorkDayResponse.Total/OpenCount),
// the site (GET /api/sites/{id}/summary) and the overall bar (GET /api/sites/summaries). Pure: no EF, no clock.
// Every item is rounded to cents first (WorkDayRules.Amount), then summed, so the site's Earned is always
// the sum of its days' totals.
public static class SiteSummaryCalculator
{
    public static SiteSummary Calculate(IEnumerable<SummaryItem> items, IEnumerable<decimal> payments)
    {
        var list = items as IReadOnlyCollection<SummaryItem> ?? [.. items];

        // „цена?“ has no amount and adds nothing; a flagged item with a price still counts in Earned.
        var earned = WorkDayRules.Total(list.Select(i => WorkDayRules.Amount(i.Quantity, i.Rate)));
        var openCount = list.Count(i => WorkDayRules.IsOpen(i.Rate, i.NeedsReview));
        var paid = payments.Sum();

        return new SiteSummary(earned, paid, earned - paid, openCount);
    }

    // The overall bar (CLAUDE.md "Плащания и суми"): Earned, Paid and OpenCount are sums over the sites.
    // „Дължат ни“ (Owed) sums only the positive remainders: an overpaid site does not lower what another site owes.
    // The overpayments are summed apart, with the number of overpaid sites. There is deliberately no netted total.
    public static SummaryTotals Total(IEnumerable<SiteSummary> sites)
    {
        var list = sites as IReadOnlyCollection<SiteSummary> ?? [.. sites];

        return new SummaryTotals(
            list.Sum(s => s.Earned),
            list.Sum(s => s.Paid),
            list.Sum(s => Math.Max(s.Remaining, 0m)),
            list.Sum(s => Math.Max(-s.Remaining, 0m)),
            list.Count(s => s.Remaining < 0m),
            list.Sum(s => s.OpenCount));
    }
}
