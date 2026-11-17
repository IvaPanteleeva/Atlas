namespace Atlas.Application.Portal;

/// <summary>A payment for the client: only the date and the amount (no note).</summary>
/// <param name="Date">Payment date.</param>
/// <param name="Amount">Amount in EUR.</param>
public sealed record PortalPaymentResponse(
    DateOnly Date,
    decimal Amount);
