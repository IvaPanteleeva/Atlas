namespace Atlas.Infrastructure.Entities;

// The price agreed for a service on one site. One per service and site.
// Not history: it goes with the site, and a change applies only to new work days (they keep a copy).
public class SitePrice
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public Site? Site { get; set; }

    public Guid ServiceId { get; set; }

    public Service? Service { get; set; }

    public decimal Rate { get; set; }
}
