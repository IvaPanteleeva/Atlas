namespace Atlas.Infrastructure.Persistence;

using Atlas.Application.Sites;
using Atlas.Infrastructure.Entities;

// A site with the date of its latest work day, the key of the site list order.
public sealed class SiteListRow
{
    public required Site Site { get; init; }

    public DateOnly? LastWorkDate { get; init; }
}

// The order of the site list in one place (CLAUDE.md "Плащания и суми"), for /api/sites and /portal/sites.
// Done in SQL: current sites first, then leads, then finished. Current sites go by their latest work day, newest
// first; those without days come last (Postgres puts NULL first in a descending order, hence the explicit key).
// Leads and finished sites go by name. Id breaks ties.
public static class SiteListQueries
{
    public static IOrderedQueryable<SiteListRow> OrderedForList(this IQueryable<Site> sites, AtlasDbContext data)
        => sites
            .Select(s => new SiteListRow
            {
                Site = s,
                LastWorkDate = data.WorkDays.Where(d => d.SiteId == s.Id).Max(d => (DateOnly?)d.Date),
            })
            .OrderBy(x => x.Site.Status == SiteStatus.Active ? 0 : x.Site.Status == SiteStatus.Lead ? 1 : 2)
            .ThenBy(x => x.Site.Status == SiteStatus.Active && x.LastWorkDate == null ? 1 : 0)
            .ThenByDescending(x => x.Site.Status == SiteStatus.Active ? x.LastWorkDate : null)
            .ThenBy(x => x.Site.Name)
            .ThenBy(x => x.Site.Id);
}
