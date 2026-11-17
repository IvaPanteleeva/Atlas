namespace Atlas.Application.Portal;

/// <summary>A work day for the client: no note, no people.</summary>
/// <param name="Id">The day.</param>
/// <param name="Date">The date of the work.</param>
/// <param name="Title">Title, or null.</param>
/// <param name="Total">Sum of the items with a price, in EUR.</param>
/// <param name="Items">In their own order.</param>
public sealed record PortalWorkDayResponse(
    Guid Id,
    DateOnly Date,
    string? Title,
    decimal Total,
    IReadOnlyList<PortalWorkItemResponse> Items);
