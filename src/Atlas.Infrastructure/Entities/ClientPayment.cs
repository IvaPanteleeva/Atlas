namespace Atlas.Infrastructure.Entities;

// A payment from the client company to Atlas for one site (CLAUDE.md "Плащания и суми").
// A payment for several sites is entered as separate payments. The sums are not stored: they are computed
// from the work items and the payments (SiteSummaryCalculator).
public class ClientPayment
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public Site? Site { get; set; }

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    // When the payment was entered (UTC). Orders payments of the same date, newest first; not shown in the API.
    public DateTimeOffset CreatedAt { get; set; }
}
