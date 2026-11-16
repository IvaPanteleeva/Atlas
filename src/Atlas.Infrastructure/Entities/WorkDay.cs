namespace Atlas.Infrastructure.Entities;

// Work done on a site on one date (CLAUDE.md "Работни дни"). Several days may share a date on the same site.
// The sums are not stored: they are computed from the items (WorkDayRules).
public class WorkDay
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public Site? Site { get; set; }

    public DateOnly Date { get; set; }

    public string? Title { get; set; }

    // Internal; never goes out through /portal/... (CLAUDE.md "Достъп за възложителя").
    public string? Note { get; set; }

    // When the day was entered (UTC). Orders days of the same date, newest first; not shown in the API.
    public DateTimeOffset CreatedAt { get; set; }

    public List<WorkItem> Items { get; set; } = [];

    public List<WorkDayWorker> Workers { get; set; } = [];
}
