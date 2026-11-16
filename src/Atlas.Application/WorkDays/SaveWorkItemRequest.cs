namespace Atlas.Application.WorkDays;

using Atlas.Application.Common;

/// <summary>One item of a work day in <see cref="SaveWorkDayRequest"/>.</summary>
/// <param name="Id">The id of an item of this day to keep it; null for a new item.
/// An id that is not an item of this day is treated as a new item.</param>
/// <param name="ServiceId">The service the item comes from (for reference only), or null for free text.</param>
/// <param name="Description">Required, up to 200 characters.</param>
/// <param name="Quantity">More than 0, up to 100 000, at most 3 decimals.</param>
/// <param name="Unit">Required (nullable only so a missing unit is a 400).</param>
/// <param name="Rate">Price in EUR or null („цена?“), stored as sent (Owner and Manager).</param>
/// <param name="NeedsReview">„За уточняване“ flag.</param>
public sealed record SaveWorkItemRequest(
    Guid? Id,
    Guid? ServiceId,
    string? Description,
    decimal Quantity,
    Unit? Unit,
    decimal? Rate,
    bool NeedsReview);
