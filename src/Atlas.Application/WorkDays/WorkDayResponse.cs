namespace Atlas.Application.WorkDays;

/// <summary>A work day on a site with its items and the sums computed on the server.</summary>
/// <param name="Id">Work day id.</param>
/// <param name="SiteId">The site.</param>
/// <param name="Date">The date of the work ("yyyy-MM-dd").</param>
/// <param name="Title">Optional title.</param>
/// <param name="Note">Internal note.</param>
/// <param name="Items">The items, in their order.</param>
/// <param name="Total">Sum of the items that have a price, in EUR.</param>
/// <param name="OpenCount">Items to clarify: without a price or flagged NeedsReview.</param>
/// <param name="Workers">Who worked, by name.</param>
public sealed record WorkDayResponse(
    Guid Id,
    Guid SiteId,
    DateOnly Date,
    string? Title,
    string? Note,
    IReadOnlyList<WorkItemResponse> Items,
    decimal Total,
    int OpenCount,
    IReadOnlyList<WorkerResponse> Workers);
