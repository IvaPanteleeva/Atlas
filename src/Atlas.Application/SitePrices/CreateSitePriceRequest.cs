namespace Atlas.Application.SitePrices;

/// <summary>Payload for adding a price for a service on a site (Owner only). One price per service and site.</summary>
/// <param name="ServiceId">An existing service.</param>
/// <param name="Rate">Required price in EUR: more than 0, up to 100 000, at most 2 decimals (nullable only so a missing value is a 400).</param>
public sealed record CreateSitePriceRequest(Guid ServiceId, decimal? Rate);
