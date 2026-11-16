namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.SiteContacts;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<SiteContact> needs the SiteContact type.
public interface ISiteContactRepository : IRepository<SiteContact>
{
    // The contacts of one site, by name. NotFound when there is no such site.
    Task<Result<IReadOnlyList<SiteContactResponse>>> ListAsync(Guid siteId, CancellationToken cancellationToken);

    // NotFound when there is no such site. Text is stored trimmed, empty → null.
    Task<Result<SiteContactResponse>> CreateAsync(
        Guid siteId,
        SaveSiteContactRequest request,
        CancellationToken cancellationToken);

    // Replaces all fields. NotFound when the contact does not exist or belongs to another site.
    Task<Result<SiteContactResponse>> UpdateAsync(
        Guid siteId,
        Guid id,
        SaveSiteContactRequest request,
        CancellationToken cancellationToken);

    // NotFound when the contact does not exist or belongs to another site.
    Task<Result> DeleteAsync(Guid siteId, Guid id, CancellationToken cancellationToken);
}
