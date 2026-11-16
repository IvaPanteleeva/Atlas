namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.SitePrices;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<SitePrice> needs the SitePrice type.
public interface ISitePriceRepository : IRepository<SitePrice>
{
    // The prices of one site, by service name. NotFound when there is no such site.
    Task<Result<IReadOnlyList<SitePriceResponse>>> ListAsync(Guid siteId, CancellationToken cancellationToken);

    // NotFound when there is no such site; Validation on serviceId when the service does not exist;
    // Conflict when the site already has a price for that service.
    Task<Result<SitePriceResponse>> CreateAsync(
        Guid siteId,
        CreateSitePriceRequest request,
        CancellationToken cancellationToken);

    // NotFound when the price does not exist or belongs to another site.
    Task<Result<SitePriceResponse>> UpdateAsync(
        Guid siteId,
        Guid priceId,
        UpdateSitePriceRequest request,
        CancellationToken cancellationToken);

    // NotFound when the price does not exist or belongs to another site.
    Task<Result> DeleteAsync(Guid siteId, Guid priceId, CancellationToken cancellationToken);
}
