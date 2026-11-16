namespace Atlas.Application.WorkDays;

/// <summary>
/// Payload for adding or editing a work day (Owner or Manager). On edit the item list replaces the old one.
/// Text is trimmed; empty title or note is stored as null.
/// </summary>
/// <param name="Date">Required: today or earlier, Bulgarian time (nullable only so a missing date is a 400).</param>
/// <param name="Title">Up to 200 characters.</param>
/// <param name="Note">Internal note, up to 2000 characters.</param>
/// <param name="Items">1 to 50 items, kept in this order.</param>
/// <param name="WorkerIds">Who worked (person ids); repeated ids count once. New workers must be crew; people already
/// on the day stay even if they are no longer crew. Null keeps the day's workers as they are; [] removes them.</param>
public sealed record SaveWorkDayRequest(
    DateOnly? Date,
    string? Title,
    string? Note,
    IReadOnlyList<SaveWorkItemRequest>? Items,
    IReadOnlyList<Guid>? WorkerIds = null);
