namespace Atlas.Application.SitePrices;

using Atlas.Application.Common;

/// <summary>The price agreed for a service on one site.</summary>
/// <param name="Id">Price id.</param>
/// <param name="ServiceId">The service.</param>
/// <param name="ServiceName">Name of the service.</param>
/// <param name="Unit">Unit of the service.</param>
/// <param name="Rate">The price for this site, in EUR.</param>
/// <param name="DefaultRate">The default price of the service (for comparison); null = none.</param>
public sealed record SitePriceResponse(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    Unit Unit,
    decimal Rate,
    decimal? DefaultRate);
