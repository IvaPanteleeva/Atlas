namespace Atlas.Application.Portal;

/// <summary>The sums of one site for the client: no worker cost, no „Остава за нас“.</summary>
/// <param name="Earned">„Изработено“: items with a price (flagged ones included), in EUR.</param>
/// <param name="Paid">„Платено“, in EUR.</param>
/// <param name="Remaining">Earned − Paid; negative means overpaid.</param>
/// <param name="OpenCount">Items without a price or flagged („за уточняване“).</param>
public sealed record PortalSummaryResponse(
    decimal Earned,
    decimal Paid,
    decimal Remaining,
    int OpenCount);
