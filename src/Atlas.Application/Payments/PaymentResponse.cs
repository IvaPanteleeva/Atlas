namespace Atlas.Application.Payments;

/// <summary>A payment from the client company to Atlas for one site.</summary>
/// <param name="Id">Payment id.</param>
/// <param name="SiteId">The site it was paid for.</param>
/// <param name="Date">When it was paid ("yyyy-MM-dd").</param>
/// <param name="Amount">Amount in EUR.</param>
/// <param name="Note">Optional note (e.g. „аванс, в брой“).</param>
public sealed record PaymentResponse(
    Guid Id,
    Guid SiteId,
    DateOnly Date,
    decimal Amount,
    string? Note);
