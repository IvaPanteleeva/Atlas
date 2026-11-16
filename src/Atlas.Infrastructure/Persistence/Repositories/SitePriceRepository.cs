namespace Atlas.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Atlas.Application.Common;
using Atlas.Application.SitePrices;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class SitePriceRepository(AtlasDbContext data) : DataRepository<SitePrice>(data), ISitePriceRepository
{
    public const string NotFoundMessage = "Няма такава цена.";

    public const string DuplicateMessage = "Тази услуга вече има цена за обекта";

    private static readonly Expression<Func<SitePrice, SitePriceResponse>> ToResponse = p => new SitePriceResponse(
        p.Id,
        p.ServiceId,
        p.Service!.Name,
        p.Service.Unit,
        p.Rate,
        p.Service.DefaultRate);

    public async Task<Result<IReadOnlyList<SitePriceResponse>>> ListAsync(
        Guid siteId,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        var prices = await this.Data.SitePrices
            .AsNoTracking()
            .Where(p => p.SiteId == siteId)
            .OrderBy(p => p.Service!.Name)
            .ThenBy(p => p.Id)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SitePriceResponse>>.SuccessWith(prices);
    }

    public async Task<Result<SitePriceResponse>> CreateAsync(
        Guid siteId,
        CreateSitePriceRequest request,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        if (!await this.Data.Services.AnyAsync(s => s.Id == request.ServiceId, cancellationToken))
        {
            return ServiceNotFound();
        }

        if (await this.Data.SitePrices.AnyAsync(
                p => p.SiteId == siteId && p.ServiceId == request.ServiceId,
                cancellationToken))
        {
            return Error.Conflict(DuplicateMessage);
        }

        // The validator makes Rate required; the fallback only keeps the compiler happy.
        var price = new SitePrice { SiteId = siteId, ServiceId = request.ServiceId, Rate = request.Rate ?? 0m };
        await this.AddAsync(price, cancellationToken);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            // The same service got a price on this site between the check and the save.
            return Error.Conflict(DuplicateMessage);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // The site or the service was deleted between the check and the save.
            this.Data.ChangeTracker.Clear();
            return await this.SiteExistsAsync(siteId, cancellationToken)
                ? ServiceNotFound()
                : Error.NotFound(SiteRepository.NotFoundMessage);
        }

        return await this.GetAsync(price.Id, cancellationToken);
    }

    public async Task<Result<SitePriceResponse>> UpdateAsync(
        Guid siteId,
        Guid priceId,
        UpdateSitePriceRequest request,
        CancellationToken cancellationToken)
    {
        var price = await this.FindAsync(siteId, priceId, cancellationToken);
        if (price is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        // Only new work days get the new price; the old ones keep their copy (CLAUDE.md "Услуги и цени по обект").
        price.Rate = request.Rate ?? price.Rate;
        await this.SaveAsync(cancellationToken);

        return await this.GetAsync(price.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid siteId, Guid priceId, CancellationToken cancellationToken)
    {
        var price = await this.FindAsync(siteId, priceId, cancellationToken);
        if (price is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        this.Delete(price);
        await this.SaveAsync(cancellationToken);

        return Result.Success;
    }

    private static Error ServiceNotFound()
        => Error.Validation(new Dictionary<string, string[]>
        {
            ["serviceId"] = [ServiceRepository.NotFoundMessage],
        });

    private async Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
        => await this.Data.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);

    // A price is only found through its own site: the id of another site's price is a 404 here.
    private async Task<SitePrice?> FindAsync(Guid siteId, Guid priceId, CancellationToken cancellationToken)
        => await this.Data.SitePrices.FirstOrDefaultAsync(
            p => p.Id == priceId && p.SiteId == siteId,
            cancellationToken);

    // Read back with the service data for the response.
    private async Task<Result<SitePriceResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var price = await this.Data.SitePrices
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return price is null ? Error.NotFound(NotFoundMessage) : price;
    }
}
