namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Sites;

// A repair site (обект). The company is optional (a direct customer has none);
// the end customer is free text (e.g. "Професорът").
public class Site
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public SiteStatus Status { get; set; } = SiteStatus.Lead;

    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public string? EndCustomer { get; set; }

    public string? Address { get; set; }

    public string? MapUrl { get; set; }

    // Internal; never goes out through /portal/... (CLAUDE.md "Достъп за възложителя").
    public string? Note { get; set; }

    // A new work day on a Lead site means the work has started (CLAUDE.md "Обекти и фирми"):
    // every time, not only the first day. A Done site stays Done (the day is extra work); Active stays Active.
    public void OnWorkDayAdded()
    {
        if (this.Status == SiteStatus.Lead)
        {
            this.Status = SiteStatus.Active;
        }
    }
}
