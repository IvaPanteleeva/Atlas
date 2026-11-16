namespace Atlas.Application.Services;

using Atlas.Application.Common;

/// <summary>A service we do (e.g. „Шпакловка“ per м²), with an optional default price.</summary>
/// <param name="Id">Service id.</param>
/// <param name="Name">Service name; unique together with the unit.</param>
/// <param name="Unit">LinearMeter, SquareMeter, Day or Piece.</param>
/// <param name="DefaultRate">Default price in EUR, used when the site has no price of its own; null = none.</param>
public sealed record ServiceResponse(Guid Id, string Name, Unit Unit, decimal? DefaultRate);
