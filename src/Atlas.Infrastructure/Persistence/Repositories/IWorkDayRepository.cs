namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<WorkDay> needs the WorkDay type.
// Prices of the items are stored as sent, for the Owner and the Manager alike (CLAUDE.md "Работни дни").
public interface IWorkDayRepository : IRepository<WorkDay>
{
    // The days of one site with their items, newest first (date, then entry time). NotFound when there is no such site.
    Task<Result<IReadOnlyList<WorkDayResponse>>> ListAsync(Guid siteId, CancellationToken cancellationToken);

    // NotFound when the day does not exist or belongs to another site.
    Task<Result<WorkDayResponse>> GetAsync(Guid siteId, Guid id, CancellationToken cancellationToken);

    // NotFound when there is no such site; Validation on items[i].serviceId for an unknown service.
    // A Lead site becomes Active in the same save.
    Task<Result<WorkDayResponse>> CreateAsync(
        Guid siteId,
        SaveWorkDayRequest request,
        CancellationToken cancellationToken);

    // Replaces the day's fields and items. NotFound when the day does not exist or belongs to another site;
    // Validation on items[i].serviceId for an unknown service. The site status is not touched.
    Task<Result<WorkDayResponse>> UpdateAsync(
        Guid siteId,
        Guid id,
        SaveWorkDayRequest request,
        CancellationToken cancellationToken);

    // NotFound when the day does not exist or belongs to another site.
    Task<Result> DeleteAsync(Guid siteId, Guid id, CancellationToken cancellationToken);
}
