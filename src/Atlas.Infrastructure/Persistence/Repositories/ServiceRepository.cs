namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Services;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class ServiceRepository(AtlasDbContext data) : DataRepository<Service>(data), IServiceRepository
{
    public const string NotFoundMessage = "Няма такава услуга.";

    public const string DuplicateMessage = "Тази услуга вече я има";

    public async Task<IReadOnlyList<ServiceResponse>> ListAsync(CancellationToken cancellationToken)
        => await this.Data.Services
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Unit == Unit.LinearMeter ? 0 : s.Unit == Unit.SquareMeter ? 1 : s.Unit == Unit.Day ? 2 : 3)
            .Select(s => new ServiceResponse(s.Id, s.Name, s.Unit, s.DefaultRate))
            .ToListAsync(cancellationToken);

    public async Task<Result<ServiceResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await this.Data.Services
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ServiceResponse(s.Id, s.Name, s.Unit, s.DefaultRate))
            .FirstOrDefaultAsync(cancellationToken);

        return service is null ? Error.NotFound(NotFoundMessage) : service;
    }

    public async Task<Result<ServiceResponse>> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var name = ServiceRules.NormalizeName(request.Name);

        // The validator makes Unit required; the fallback only keeps the compiler happy.
        var unit = request.Unit ?? default;

        if (await this.IsDuplicateAsync(name, unit, exceptId: null, cancellationToken))
        {
            return Error.Conflict(DuplicateMessage);
        }

        var service = new Service { Name = name, Unit = unit, DefaultRate = request.DefaultRate };
        await this.AddAsync(service, cancellationToken);

        return await this.SaveAsync(service, cancellationToken);
    }

    public async Task<Result<ServiceResponse>> UpdateAsync(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await this.Data.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        var name = ServiceRules.NormalizeName(request.Name);
        var unit = request.Unit ?? service.Unit;

        if (await this.IsDuplicateAsync(name, unit, exceptId: id, cancellationToken))
        {
            return Error.Conflict(DuplicateMessage);
        }

        // Prices already copied into work days stay as they are (CLAUDE.md "Правила за домейна").
        service.Name = name;
        service.Unit = unit;
        service.DefaultRate = request.DefaultRate;

        return await this.SaveAsync(service, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await this.Data.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        // Only site prices block the deletion; work days keep their own copy (CLAUDE.md "Услуги и цени по обект").
        var siteCount = await this.Data.SitePrices.CountAsync(p => p.ServiceId == id, cancellationToken);
        if (siteCount > 0)
        {
            return Error.Conflict(ServiceRules.InUseMessage(siteCount));
        }

        this.Delete(service);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // A price for the service was added after the check (the FK is Restrict).
            this.Data.ChangeTracker.Clear();
            var count = await this.Data.SitePrices.CountAsync(p => p.ServiceId == id, cancellationToken);
            return Error.Conflict(ServiceRules.InUseMessage(Math.Max(count, 1)));
        }

        return Result.Success;
    }

    // Case-insensitive, like company names: „Шпакловка“ and „ШПАКЛОВКА“ per м² are the same service.
    // The same name with another unit is a different service. Postgres lower() handles Cyrillic.
    private async Task<bool> IsDuplicateAsync(
        string name,
        Unit unit,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var lowered = name.ToLowerInvariant();
        return await this.Data.Services.AnyAsync(
            s => s.Name.ToLower() == lowered && s.Unit == unit && s.Id != exceptId,
            cancellationToken);
    }

    // The unique index (Name, Unit) still catches a duplicate added between the check and the save.
    private async Task<Result<ServiceResponse>> SaveAsync(Service service, CancellationToken cancellationToken)
    {
        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            return Error.Conflict(DuplicateMessage);
        }

        return new ServiceResponse(service.Id, service.Name, service.Unit, service.DefaultRate);
    }
}
