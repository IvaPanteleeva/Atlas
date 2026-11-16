namespace Atlas.Application.WorkDays;

using Atlas.Application.Common;

/// <summary>One item of a work day. It keeps its own copy of the price.</summary>
/// <param name="Id">Item id.</param>
/// <param name="ServiceId">The service it came from, or null (free text or the service was deleted).</param>
/// <param name="Description">What was done.</param>
/// <param name="Quantity">How much.</param>
/// <param name="Unit">Unit of measure.</param>
/// <param name="Rate">Price in EUR, or null („цена?“).</param>
/// <param name="NeedsReview">„За уточняване“ flag.</param>
/// <param name="Amount">Quantity × Rate rounded to cents, or null without a price.</param>
public sealed record WorkItemResponse(
    Guid Id,
    Guid? ServiceId,
    string Description,
    decimal Quantity,
    Unit Unit,
    decimal? Rate,
    bool NeedsReview,
    decimal? Amount);
