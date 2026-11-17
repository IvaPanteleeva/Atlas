namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Portal;
using Atlas.Application.Summaries;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

// The sums come from SiteSummaryCalculator and the amounts from WorkDayRules, so the client sees the same numbers
// as Owner and Manager. Worker pay is never loaded: the portal has no worker cost and no „Остава за нас“.
public sealed class PortalRepository(AtlasDbContext data) : DataRepository<Site>(data), IPortalRepository
{
    public async Task<PortalSitesResponse> ListAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var sites = await this.CompanySites(companyId)
            .AsNoTracking()
            .OrderedForList(this.Data)
            .Select(x => new { x.Site.Id, x.Site.Name, x.Site.EndCustomer, x.Site.Status })
            .ToListAsync(cancellationToken);

        var items = (await this.Data.WorkItems
                .AsNoTracking()
                .Where(i => i.WorkDay!.Site!.CompanyId == companyId)
                .Select(i => new { i.WorkDay!.SiteId, i.Quantity, i.Rate, i.NeedsReview })
                .ToListAsync(cancellationToken))
            .ToLookup(i => i.SiteId, i => new SummaryItem(i.Quantity, i.Rate, i.NeedsReview));

        var paid = await this.Data.ClientPayments
            .AsNoTracking()
            .Where(p => p.Site!.CompanyId == companyId)
            .GroupBy(p => p.SiteId)
            .Select(g => new { SiteId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(p => p.SiteId, p => p.Paid, cancellationToken);

        var rows = sites
            .Select(s => (Site: s, Summary: SiteSummaryCalculator.Calculate(
                items[s.Id],
                paid.TryGetValue(s.Id, out var sum) ? [sum] : [])))
            .ToList();

        // „Дължите“ = the positive remainders only; overpayments apart (the same rule as the internal bar).
        var totals = SiteSummaryCalculator.Total(rows.Select(r => r.Summary));

        return new PortalSitesResponse(
            [.. rows.Select(r => new PortalSiteListResponse(
                r.Site.Id,
                r.Site.Name,
                r.Site.EndCustomer,
                r.Site.Status,
                r.Summary.Earned,
                r.Summary.Paid,
                r.Summary.Remaining,
                r.Summary.OpenCount))],
            new PortalTotalsResponse(totals.Owed, totals.Overpaid, totals.OverpaidSiteCount));
    }

    public async Task<Result<PortalSiteResponse>> GetAsync(
        Guid companyId,
        Guid siteId,
        CancellationToken cancellationToken)
    {
        // A site of another company looks exactly like a site that does not exist.
        var site = await this.CompanySites(companyId)
            .AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(s => new { s.Id, s.Name, s.EndCustomer, s.Address, s.MapUrl, s.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (site is null)
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        // Newest first, like the internal list (date, then the one entered later; Id only makes ties stable).
        var days = await this.Data.WorkDays
            .AsNoTracking()
            .Where(d => d.SiteId == site.Id)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .Select(d => new
            {
                d.Id,
                d.Date,
                d.Title,
                Items = d.Items
                    .OrderBy(i => i.Position)
                    .Select(i => new { i.Description, i.Quantity, i.Unit, i.Rate, i.NeedsReview })
                    .ToList(),
            })
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var payments = await this.Data.ClientPayments
            .AsNoTracking()
            .Where(p => p.SiteId == site.Id)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PortalPaymentResponse(p.Date, p.Amount))
            .ToListAsync(cancellationToken);

        var workDays = days
            .Select(d => new PortalWorkDayResponse(
                d.Id,
                d.Date,
                d.Title,
                SiteSummaryCalculator.Calculate(
                    d.Items.Select(i => new SummaryItem(i.Quantity, i.Rate, i.NeedsReview)),
                    []).Earned,
                [.. d.Items.Select(i => new PortalWorkItemResponse(
                    i.Description,
                    i.Quantity,
                    i.Unit,
                    i.Rate,
                    i.NeedsReview,
                    WorkDayRules.Amount(i.Quantity, i.Rate)))]))
            .ToList();

        var summary = SiteSummaryCalculator.Calculate(
            days.SelectMany(d => d.Items).Select(i => new SummaryItem(i.Quantity, i.Rate, i.NeedsReview)),
            payments.Select(p => p.Amount));

        return new PortalSiteResponse(
            site.Id,
            site.Name,
            site.EndCustomer,
            site.Address,
            site.MapUrl,
            site.Status,
            new PortalSummaryResponse(summary.Earned, summary.Paid, summary.Remaining, summary.OpenCount),
            workDays,
            payments);
    }

    // The only way into the sites here: a site without a company (CompanyId null) never matches.
    private IQueryable<Site> CompanySites(Guid companyId)
        => this.Data.Sites.Where(s => s.CompanyId == companyId);
}
