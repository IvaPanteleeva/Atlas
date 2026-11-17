namespace Atlas.Application.WorkDays;

/// <summary>One person on a work day in <see cref="SaveWorkDayRequest"/>.</summary>
/// <param name="PersonId">The person.</param>
/// <param name="Pay">What the person gets for this day in EUR, stored as sent (a copy: changing the daily rate later
/// does not change it). Null = „за уточняване“: not in the sums, counted apart. More than 0, up to 100 000,
/// at most 2 decimals.</param>
public sealed record SaveWorkerRequest(Guid PersonId, decimal? Pay);
