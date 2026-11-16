namespace Atlas.Application.Services;

using Atlas.Application.Common;

/// <summary>Payload for adding a service (Owner only). The name is trimmed.</summary>
/// <param name="Name">Required, up to 200 characters; unique together with the unit (case-insensitive).</param>
/// <param name="Unit">Required: LinearMeter, SquareMeter, Day or Piece (nullable only so a missing value is a 400).</param>
/// <param name="DefaultRate">Optional default price in EUR: more than 0, up to 100 000, at most 2 decimals.</param>
public sealed record CreateServiceRequest(string Name, Unit? Unit, decimal? DefaultRate = null);
