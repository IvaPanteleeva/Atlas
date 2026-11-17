namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Summaries;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

// The sums are computed on every request, never stored (CLAUDE.md "Правила за домейна", "Разход за хора").
// SQL brings only quantity, price and flag of the items and the payment sums; the rounding per item and
// the sums are SiteSummaryCalculator's job, the same rule as WorkDayResponse.Total.
public sealed class SiteSummaryRepository(AtlasDbContext data) : DataRepository<Site>(data), ISiteSummaryRepository
{
    public async Task<Result<SiteSummaryResponse>> GetAsync(Guid siteId, CancellationToken cancellationToken)
    {
        // One query: does the site exist, its days and its payment sum.
        var site = await this.Data.Sites
            .AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(s => new
            {
                WorkDayCount = this.Data.WorkDays.Count(d => d.SiteId == s.Id),
                LastWorkDate = this.Data.WorkDays.Where(d => d.SiteId == s.Id).Max(d => (DateOnly?)d.Date),
                Paid = this.Data.ClientPayments.Where(p => p.SiteId == s.Id).Sum(p => (decimal?)p.Amount) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (site is null)
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        var items = await this.Data.WorkItems
            .AsNoTracking()
            .Where(i => i.WorkDay!.SiteId == siteId)
            .Select(i => new SummaryItem(i.Quantity, i.Rate, i.NeedsReview))
            .ToListAsync(cancellationToken);

        // One pay per person-day on the site (null = „за уточняване“, unless the person is now a partner).
        var partners = this.Data.PartnerPersonIds();
        var pays = await this.Data.WorkDayWorkers
            .AsNoTracking()
            .Where(w => w.WorkDay!.SiteId == siteId)
            .Select(w => new SummaryWorker(w.Pay, partners.Contains(w.PersonId)))
            .ToListAsync(cancellationToken);

        var summary = SiteSummaryCalculator.Calculate(items, [site.Paid], pays);

        return new SiteSummaryResponse(
            summary.Earned,
            summary.Paid,
            summary.Remaining,
            summary.OpenCount,
            site.WorkDayCount,
            site.LastWorkDate,
            summary.WorkerCost,
            summary.WorkerOpenCount,
            summary.Net);
    }

    public async Task<SiteSummariesResponse> ListAsync(CancellationToken cancellationToken)
    {
        var siteIds = await this.Data.Sites
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var items = (await this.Data.WorkItems
                .AsNoTracking()
                .Select(i => new { i.WorkDay!.SiteId, i.Quantity, i.Rate, i.NeedsReview })
                .ToListAsync(cancellationToken))
            .ToLookup(i => i.SiteId, i => new SummaryItem(i.Quantity, i.Rate, i.NeedsReview));

        var paid = await this.Data.ClientPayments
            .AsNoTracking()
            .GroupBy(p => p.SiteId)
            .Select(g => new { SiteId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(p => p.SiteId, p => p.Paid, cancellationToken);

        var partners = this.Data.PartnerPersonIds();
        var pays = (await this.Data.WorkDayWorkers
                .AsNoTracking()
                .Select(w => new { w.WorkDay!.SiteId, w.Pay, IsPartner = partners.Contains(w.PersonId) })
                .ToListAsync(cancellationToken))
            .ToLookup(w => w.SiteId, w => new SummaryWorker(w.Pay, w.IsPartner));

        var sites = siteIds
            .Select(id => (Id: id, Summary: SiteSummaryCalculator.Calculate(
                items[id],
                paid.TryGetValue(id, out var sum) ? [sum] : [],
                pays[id])))
            .ToList();

        // The totals come from the sites' sums, so the bar always matches the rows (Owed and Overpaid kept apart).
        var totals = SiteSummaryCalculator.Total(sites.Select(s => s.Summary));

        return new SiteSummariesResponse(
            new SummaryTotalsResponse(
                totals.Earned,
                totals.Paid,
                totals.Owed,
                totals.Overpaid,
                totals.OverpaidSiteCount,
                totals.OpenCount,
                totals.WorkerCost,
                totals.WorkerOpenCount,
                totals.Net),
            [.. sites.Select(s => new SiteSummaryListResponse(
                s.Id,
                s.Summary.Earned,
                s.Summary.Paid,
                s.Summary.Remaining,
                s.Summary.OpenCount,
                s.Summary.WorkerCost,
                s.Summary.WorkerOpenCount,
                s.Summary.Net))]);
    }
}
