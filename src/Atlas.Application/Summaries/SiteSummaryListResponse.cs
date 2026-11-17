namespace Atlas.Application.Summaries;

/// <summary>The sums of one site in <see cref="SiteSummariesResponse"/>.</summary>
/// <param name="SiteId">The site.</param>
/// <param name="Earned">„Изработено“ in EUR.</param>
/// <param name="Paid">„Платено“ in EUR.</param>
/// <param name="Remaining">Earned − Paid; negative means overpaid.</param>
/// <param name="OpenCount">Items without a price or flagged NeedsReview.</param>
/// <param name="WorkerCost">„Разход за хора“: sum of the pay on the site's days.</param>
/// <param name="WorkerOpenCount">People-days without a pay.</param>
/// <param name="Net">„Остава за нас“: Earned − WorkerCost; may be negative.</param>
public sealed record SiteSummaryListResponse(
    Guid SiteId,
    decimal Earned,
    decimal Paid,
    decimal Remaining,
    int OpenCount,
    decimal WorkerCost,
    int WorkerOpenCount,
    decimal Net);
