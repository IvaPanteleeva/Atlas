namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Identity;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class WorkDayRepository(AtlasDbContext data, ICurrentUser currentUser, TimeProvider timeProvider)
    : DataRepository<WorkDay>(data), IWorkDayRepository
{
    public const string NotFoundMessage = "Няма такъв работен ден.";

    public async Task<Result<IReadOnlyList<WorkDayResponse>>> ListAsync(
        Guid siteId,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        // Newest first: by date, then the one entered later (CLAUDE.md "Работни дни"). Id only makes ties stable.
        // Amounts and sums are computed in memory (WorkDayRules), so the items are loaded, not projected.
        var days = await this.Data.WorkDays
            .AsNoTracking()
            .Include(d => d.Items)
            .Where(d => d.SiteId == siteId)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WorkDayResponse>>.SuccessWith([.. days.Select(ToResponse)]);
    }

    public async Task<Result<WorkDayResponse>> GetAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
    {
        var day = await this.Data.WorkDays
            .AsNoTracking()
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id && d.SiteId == siteId, cancellationToken);

        return day is null ? Error.NotFound(NotFoundMessage) : ToResponse(day);
    }

    public async Task<Result<WorkDayResponse>> CreateAsync(
        Guid siteId,
        SaveWorkDayRequest request,
        CancellationToken cancellationToken)
    {
        var site = await this.Data.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
        if (site is null)
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        var prices = await this.LoadPricesAsync(siteId, Items(request), cancellationToken);
        if (prices.Error is { } error)
        {
            return error;
        }

        var day = new WorkDay { SiteId = siteId, CreatedAt = timeProvider.GetUtcNow() };
        this.Apply(day, request, prices);

        // The same save as the day: a Lead site becomes Active (CLAUDE.md "Обекти и фирми").
        site.OnWorkDayAdded();

        await this.AddAsync(day, cancellationToken);

        return await this.SaveAsync(siteId, day, request, cancellationToken);
    }

    public async Task<Result<WorkDayResponse>> UpdateAsync(
        Guid siteId,
        Guid id,
        SaveWorkDayRequest request,
        CancellationToken cancellationToken)
    {
        var day = await this.Data.WorkDays
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id && d.SiteId == siteId, cancellationToken);
        if (day is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        var prices = await this.LoadPricesAsync(siteId, Items(request), cancellationToken);
        if (prices.Error is { } error)
        {
            return error;
        }

        // Only a new day moves the site from Lead to Active; editing does not.
        this.Apply(day, request, prices);

        return await this.SaveAsync(siteId, day, request, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
    {
        var day = await this.Data.WorkDays.FirstOrDefaultAsync(d => d.Id == id && d.SiteId == siteId, cancellationToken);
        if (day is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        // The items go with it (cascade in the database).
        this.Delete(day);
        await this.SaveAsync(cancellationToken);

        return Result.Success;
    }

    private bool IsOwner => currentUser.Role == Roles.Owner;

    // The validator makes Items required; the fallback only keeps the compiler happy.
    private static IReadOnlyList<SaveWorkItemRequest> Items(SaveWorkDayRequest request)
        => request.Items ?? [];

    private static WorkDayResponse ToResponse(WorkDay day)
    {
        var items = day.Items
            .OrderBy(i => i.Position)
            .Select(i => new WorkItemResponse(
                i.Id,
                i.ServiceId,
                i.Description,
                i.Quantity,
                i.Unit,
                i.Rate,
                i.NeedsReview,
                WorkDayRules.Amount(i.Quantity, i.Rate)))
            .ToList();

        return new WorkDayResponse(
            day.Id,
            day.SiteId,
            day.Date,
            day.Title,
            day.Note,
            items,
            WorkDayRules.Total(items.Select(i => i.Amount)),
            items.Count(i => WorkDayRules.IsOpen(i.Rate, i.NeedsReview)));
    }

    // Copies the request onto the day. The item list replaces the old one:
    // an item whose Id is an item of this day is updated in place (keeps its Id and, for Crew, its price);
    // any other item is new (an Id of another day's item is ignored, the item gets a new Id);
    // the old items that are not in the request are deleted.
    private void Apply(WorkDay day, SaveWorkDayRequest request, ServicePrices prices)
    {
        // The validator makes Date required; the fallback only keeps the compiler happy.
        day.Date = request.Date ?? day.Date;
        day.Title = WorkDayRules.NormalizeOptional(request.Title);
        day.Note = WorkDayRules.NormalizeOptional(request.Note);

        var old = day.Items.ToDictionary(i => i.Id);
        var items = Items(request);

        for (var position = 0; position < items.Count; position++)
        {
            var source = items[position];

            // Remove, so the same Id twice in a request updates the item once and adds the second as new.
            var item = source.Id is { } itemId && old.Remove(itemId, out var found) ? found : null;
            var isExisting = item is not null;
            var storedRate = item?.Rate;

            // The validator makes Description and Unit required; the fallbacks only keep the compiler happy.
            item ??= new WorkItem { Description = string.Empty };
            item.ServiceId = source.ServiceId;
            item.Description = source.Description?.Trim() ?? string.Empty;
            item.Quantity = source.Quantity;
            item.Unit = source.Unit ?? default;
            item.NeedsReview = source.NeedsReview;
            item.Position = position;
            item.Rate = WorkDayRules.ResolveRate(
                this.IsOwner,
                source.Rate,
                isExisting,
                storedRate,
                prices.SitePrice(source.ServiceId),
                prices.DefaultRate(source.ServiceId));

            if (!isExisting)
            {
                // Id stays empty: EF generates it and inserts the item.
                day.Items.Add(item);
            }
        }

        foreach (var removed in old.Values)
        {
            day.Items.Remove(removed);
            this.Data.WorkItems.Remove(removed);
        }
    }

    // The services named in the request with their default price and the site's price.
    // An unknown service is a 400 on that item (items[i].serviceId), for the Owner and Crew alike.
    private async Task<ServicePrices> LoadPricesAsync(
        Guid siteId,
        IReadOnlyList<SaveWorkItemRequest> items,
        CancellationToken cancellationToken)
    {
        var serviceIds = items
            .Select(i => i.ServiceId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        if (serviceIds.Count == 0)
        {
            return ServicePrices.None;
        }

        var defaults = await this.Data.Services
            .AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.DefaultRate, cancellationToken);

        var errors = items
            .Select((item, index) => (item, index))
            .Where(x => x.item.ServiceId is { } id && !defaults.ContainsKey(id))
            .ToDictionary(
                x => $"items[{x.index}].serviceId",
                _ => new[] { ServiceRepository.NotFoundMessage });
        if (errors.Count > 0)
        {
            return new ServicePrices(defaults, new Dictionary<Guid, decimal>(), Error.Validation(errors));
        }

        var sitePrices = await this.Data.SitePrices
            .AsNoTracking()
            .Where(p => p.SiteId == siteId && serviceIds.Contains(p.ServiceId))
            .ToDictionaryAsync(p => p.ServiceId, p => p.Rate, cancellationToken);

        return new ServicePrices(defaults, sitePrices, null);
    }

    private async Task<Result<WorkDayResponse>> SaveAsync(
        Guid siteId,
        WorkDay day,
        SaveWorkDayRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // The site or a service was deleted between the checks and the save.
            this.Data.ChangeTracker.Clear();
            if (!await this.SiteExistsAsync(siteId, cancellationToken))
            {
                return Error.NotFound(SiteRepository.NotFoundMessage);
            }

            var prices = await this.LoadPricesAsync(siteId, Items(request), cancellationToken);
            if (prices.Error is { } error)
            {
                return error;
            }

            throw;
        }

        return ToResponse(day);
    }

    private async Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
        => await this.Data.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);

    private sealed record ServicePrices(
        IReadOnlyDictionary<Guid, decimal?> Defaults,
        IReadOnlyDictionary<Guid, decimal> SitePrices,
        Error? Error)
    {
        public static readonly ServicePrices None = new(new Dictionary<Guid, decimal?>(), new Dictionary<Guid, decimal>(), null);

        public decimal? SitePrice(Guid? serviceId)
            => serviceId is { } id && this.SitePrices.TryGetValue(id, out var rate) ? rate : null;

        public decimal? DefaultRate(Guid? serviceId)
            => serviceId is { } id && this.Defaults.TryGetValue(id, out var rate) ? rate : null;
    }
}
