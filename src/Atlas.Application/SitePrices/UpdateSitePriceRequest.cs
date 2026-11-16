namespace Atlas.Application.SitePrices;

/// <summary>Payload for changing a site price (Owner only). The service stays the same.</summary>
/// <param name="Rate">Required price in EUR: more than 0, up to 100 000, at most 2 decimals (nullable only so a missing value is a 400).</param>
public sealed record UpdateSitePriceRequest(decimal? Rate);
