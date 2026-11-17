namespace Atlas.Application.Summaries;

using Atlas.Application.WorkDays;

/// <summary>What the sums need from a work item: no description, no unit.</summary>
public readonly record struct SummaryItem(decimal Quantity, decimal? Rate, bool NeedsReview);

/// <summary>What the sums need from a person on a work day: the pay and whether the person is (now) a partner.</summary>
public readonly record struct SummaryWorker(decimal? Pay, bool IsPartner);

/// <summary>
/// Earned, Paid, Remaining (Earned − Paid, negative = overpaid), the open count, and the cost of our people:
/// WorkerCost (Σ the pay of the worker-days that have one), WorkerOpenCount (worker-days without a pay) and
/// Net = Earned − WorkerCost („Остава за нас“, negative allowed).
/// </summary>
public sealed record SiteSummary(
    decimal Earned,
    decimal Paid,
    decimal Remaining,
    int OpenCount,
    decimal WorkerCost,
    int WorkerOpenCount,
    decimal Net);

/// <summary>The sums over all sites: Owed and Overpaid are kept apart, never netted.</summary>
public sealed record SummaryTotals(
    decimal Earned,
    decimal Paid,
    decimal Owed,
    decimal Overpaid,
    int OverpaidSiteCount,
    int OpenCount,
    decimal WorkerCost,
    int WorkerOpenCount,
    decimal Net);

// The sums of CLAUDE.md "Плащания и суми" in one place, for the day (WorkDayResponse.Total/OpenCount),
// the site (GET /api/sites/{id}/summary) and the overall bar (GET /api/sites/summaries). Pure: no EF, no clock.
// Every item is rounded to cents first (WorkDayRules.Amount), then summed, so the site's Earned is always
// the sum of its days' totals.
public static class SiteSummaryCalculator
{
    public static SiteSummary Calculate(
        IEnumerable<SummaryItem> items,
        IEnumerable<decimal> payments,
        IEnumerable<SummaryWorker>? workers = null)
    {
        var list = items as IReadOnlyCollection<SummaryItem> ?? [.. items];

        // „цена?“ has no amount and adds nothing; a flagged item with a price still counts in Earned.
        var earned = WorkDayRules.Total(list.Select(i => WorkDayRules.Amount(i.Quantity, i.Rate)));
        var openCount = list.Count(i => WorkDayRules.IsOpen(i.Rate, i.NeedsReview));
        var paid = payments.Sum();

        // CLAUDE.md "Разход за хора": the pay of each person-day. An empty pay („за уточняване“) adds nothing and is
        // counted apart, except for partners: their pay („надник“, рунд 31) is optional. A partner's pay, when filled in,
        // is a cost like a worker's. „Остава за нас“ comes from Earned (not from Paid) and may be negative.
        var people = workers as IReadOnlyCollection<SummaryWorker> ?? [.. workers ?? []];
        var workerCost = people.Sum(w => w.Pay ?? 0m);
        var workerOpenCount = people.Count(w => w.Pay is null && !w.IsPartner);

        return new SiteSummary(earned, paid, earned - paid, openCount, workerCost, workerOpenCount, earned - workerCost);
    }

    // The overall bar (CLAUDE.md "Плащания и суми"): Earned, Paid, OpenCount and the worker sums are sums over the
    // sites (a negative Net lowers the total Net: it is our own money, not a debt between sites).
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
            list.Sum(s => s.OpenCount),
            list.Sum(s => s.WorkerCost),
            list.Sum(s => s.WorkerOpenCount),
            list.Sum(s => s.Net));
    }
}
