namespace Atlas.Application.Summaries;

/// <summary>
/// The sums over all sites (CLAUDE.md "Плащания и суми"). An overpaid site does not lower the debt of another one:
/// „Дължат ни“ sums only the positive remainders, the overpayment is shown apart. There is no netted "remaining".
/// </summary>
/// <param name="Earned">„Изработено“: sum over the sites, in EUR.</param>
/// <param name="Paid">„Платено“: sum over the sites, in EUR.</param>
/// <param name="Owed">„Дължат ни“: sum of the sites' Remaining where it is above 0.</param>
/// <param name="Overpaid">„Надплатено“: sum of the overpayments (−Remaining where Remaining is below 0).</param>
/// <param name="OverpaidSiteCount">Number of sites with Remaining below 0.</param>
/// <param name="OpenCount">Items without a price or flagged NeedsReview, on all sites.</param>
public sealed record SummaryTotalsResponse(
    decimal Earned,
    decimal Paid,
    decimal Owed,
    decimal Overpaid,
    int OverpaidSiteCount,
    int OpenCount);
