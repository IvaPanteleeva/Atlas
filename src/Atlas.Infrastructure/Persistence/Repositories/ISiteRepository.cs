namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Sites;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<Site> needs the Site type.
public interface ISiteRepository : IRepository<Site>
{
    // All sites: Active, then Lead, then Done; by name inside each group. No note.
    Task<IReadOnlyList<SiteListResponse>> ListAsync(CancellationToken cancellationToken);

    // NotFound when there is no such site.
    Task<Result<SiteResponse>> GetAsync(Guid id, CancellationToken cancellationToken);

    // Stores normalized text (trimmed, empty → null); Lead when no status is given.
    // Validation on companyId when the company does not exist.
    Task<Result<SiteResponse>> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken);

    // Replaces all fields. NotFound, or Validation on companyId when the company does not exist.
    Task<Result<SiteResponse>> UpdateAsync(Guid id, UpdateSiteRequest request, CancellationToken cancellationToken);

    // NotFound when there is no such site.
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
