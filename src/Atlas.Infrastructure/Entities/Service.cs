namespace Atlas.Infrastructure.Entities;

using Atlas.Application.Common;

// A service we do (e.g. „Шпакловка“ per м²). The default price is optional; a site can agree its own (SitePrice).
// Work items (feature 5) copy the price, they do not point to it (CLAUDE.md "Услуги и цени по обект").
public class Service
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Unit Unit { get; set; }

    public decimal? DefaultRate { get; set; }
}
