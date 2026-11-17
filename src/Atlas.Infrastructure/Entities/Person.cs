namespace Atlas.Infrastructure.Entities;

// One of our people (IsCrew: works with us) or a contact (CLAUDE.md "Хора и профили").
// Days per person and what they earned are not stored: they are computed from the work days (PersonRules).
public class Person
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Phone { get; set; }

    // Internal: only the Owner sees it; never goes out through /portal/... (CLAUDE.md "Достъп за възложителя").
    public string? Note { get; set; }

    public bool IsCrew { get; set; }

    // The usual pay for a day (CLAUDE.md "Разход за хора"). The work day form suggests it; each day keeps its own
    // copy (WorkDayWorker.Pay), so changing the rate never changes old days.
    public decimal? DailyRate { get; set; }
}
