namespace Atlas.Application.Payments;

/// <summary>
/// Payload for adding or editing a client payment (Owner only). The note is trimmed; an empty note is stored as null.
/// </summary>
/// <param name="Date">Required: today or earlier, Bulgarian time (nullable only so a missing date is a 400).</param>
/// <param name="Amount">Required: more than 0, up to 1 000 000 €, at most 2 decimals (nullable only so a missing
/// amount is a 400, not 0).</param>
/// <param name="Note">Up to 200 characters.</param>
public sealed record SavePaymentRequest(
    DateOnly? Date,
    decimal? Amount,
    string? Note);
