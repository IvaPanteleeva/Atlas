namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;

// One line of a work day. It keeps its own copy of the price (CLAUDE.md "Правила за домейна"):
// ServiceId only says where it came from, a later change of the service or the site price does not touch it.
public class WorkItem
{
    public Guid Id { get; set; }

    public Guid WorkDayId { get; set; }

    public WorkDay? WorkDay { get; set; }

    // Null for free text, or after the service was deleted.
    public Guid? ServiceId { get; set; }

    public Service? Service { get; set; }

    public required string Description { get; set; }

    public decimal Quantity { get; set; }

    public Unit Unit { get; set; }

    // Null = „цена?“: not in the sums, counted as open.
    public decimal? Rate { get; set; }

    public bool NeedsReview { get; set; }

    // Order within the day (0, 1, 2, ...), as sent in the request.
    public int Position { get; set; }
}
