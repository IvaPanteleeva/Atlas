namespace Atlas.Application.Summaries;

/// <summary>The sums of one site, computed on the server at every status (CLAUDE.md "Плащания и суми").</summary>
/// <param name="Earned">„Изработено“: sum of the items with a price (flagged ones included), in EUR.</param>
/// <param name="Paid">„Платено“: sum of the client payments, in EUR.</param>
/// <param name="Remaining">„Остатък“: Earned − Paid; negative means overpaid („Надплатено“).</param>
/// <param name="OpenCount">„За уточняване“: items without a price or flagged NeedsReview.</param>
/// <param name="WorkDayCount">Number of work days.</param>
/// <param name="LastWorkDate">Date of the latest work day, or null without days.</param>
public sealed record SiteSummaryResponse(
    decimal Earned,
    decimal Paid,
    decimal Remaining,
    int OpenCount,
    int WorkDayCount,
    DateOnly? LastWorkDate);
