namespace Atlas.Application.Portal;

using Atlas.Application.Common;

/// <summary>One item of a work day for the client.</summary>
/// <param name="Description">What was done.</param>
/// <param name="Quantity">Up to 3 decimals.</param>
/// <param name="Unit">LinearMeter, SquareMeter, Day or Piece.</param>
/// <param name="Rate">Price in EUR, or null („цена?“).</param>
/// <param name="NeedsReview">„Уточни“ flag.</param>
/// <param name="Amount">Quantity × Rate in cents, or null without a price.</param>
public sealed record PortalWorkItemResponse(
    string Description,
    decimal Quantity,
    Unit Unit,
    decimal? Rate,
    bool NeedsReview,
    decimal? Amount);
