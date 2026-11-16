namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Summaries;
using Atlas.Infrastructure.Entities;

// Read-only sums per site (CLAUDE.md "Плащания и суми"). IRepository<Site> because the sums belong to a site
// (and Scrutor registers repositories by that interface); it never adds or deletes sites.
// Loads only the columns the sums need and lets SiteSummaryCalculator compute them.
public interface ISiteSummaryRepository : IRepository<Site>
{
    // The sums of one site at any status. NotFound when there is no such site.
    Task<Result<SiteSummaryResponse>> GetAsync(Guid siteId, CancellationToken cancellationToken);

    // The sums of every site (any status, also sites without records) and the totals over them.
    Task<SiteSummariesResponse> ListAsync(CancellationToken cancellationToken);
}
