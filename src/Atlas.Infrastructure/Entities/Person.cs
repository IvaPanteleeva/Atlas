namespace Atlas.Infrastructure.Entities;

// One of our people (IsCrew: works with us) or a contact (CLAUDE.md "Хора и профили").
// Days per person are not stored: they are counted from the work days (PersonRules.WorkStats).
public class Person
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Phone { get; set; }

    // Internal: only the Owner sees it; never goes out through /portal/... (CLAUDE.md "Достъп за възложителя").
    public string? Note { get; set; }

    public bool IsCrew { get; set; }
}
