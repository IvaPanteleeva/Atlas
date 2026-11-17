namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Summaries;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class WorkDayRepository(AtlasDbContext data, TimeProvider timeProvider)
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
            .Include(d => d.Workers).ThenInclude(w => w.Person)
            .AsSplitQuery()
            .Where(d => d.SiteId == siteId)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);

        var partners = await this.Data.PartnerPersonIdSetAsync(cancellationToken);

        return Result<IReadOnlyList<WorkDayResponse>>.SuccessWith([.. days.Select(d => ToResponse(d, partners))]);
    }

    public async Task<Result<WorkDayResponse>> GetAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
    {
        var day = await this.Data.WorkDays
            .AsNoTracking()
            .Include(d => d.Items)
            .Include(d => d.Workers).ThenInclude(w => w.Person)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id && d.SiteId == siteId, cancellationToken);

        if (day is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        return ToResponse(day, await this.Data.PartnerPersonIdSetAsync(cancellationToken));
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

        if (await this.UnknownServicesAsync(Items(request), cancellationToken) is { } error)
        {
            return error;
        }

        var day = new WorkDay { SiteId = siteId, CreatedAt = timeProvider.GetUtcNow() };

        var workers = await this.LoadWorkersAsync(day, request.Workers ?? [], cancellationToken);
        if (workers.Error is { } workerError)
        {
            return workerError;
        }

        this.Apply(day, request);
        SetWorkers(day, workers.Chosen);

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
            .Include(d => d.Workers).ThenInclude(w => w.Person)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id && d.SiteId == siteId, cancellationToken);
        if (day is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        if (await this.UnknownServicesAsync(Items(request), cancellationToken) is { } error)
        {
            return error;
        }

        // No Workers (null) keeps the day's workers and their pay; a list (also empty) replaces them.
        var workers = request.Workers is { } requested
            ? await this.LoadWorkersAsync(day, requested, cancellationToken)
            : null;
        if (workers?.Error is { } workerError)
        {
            return workerError;
        }

        // Only a new day moves the site from Lead to Active; editing does not.
        this.Apply(day, request);
        if (workers is not null)
        {
            SetWorkers(day, workers.Chosen);
        }

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

    // The validator makes Items required; the fallback only keeps the compiler happy.
    private static IReadOnlyList<SaveWorkItemRequest> Items(SaveWorkDayRequest request)
        => request.Items ?? [];

    private static WorkDayResponse ToResponse(WorkDay day, IReadOnlySet<Guid> partners)
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

        // The same rule as the site's sums (SiteSummaryCalculator), so a site's Earned is the sum of its days' totals.
        var sums = SiteSummaryCalculator.Calculate(
            day.Items.Select(i => new SummaryItem(i.Quantity, i.Rate, i.NeedsReview)),
            []);

        // By name, like the people list; the id keeps two people with the same name in a stable order.
        var workers = day.Workers
            .Select(w => new WorkerResponse(w.PersonId, w.Person!.Name, w.Pay, partners.Contains(w.PersonId)))
            .OrderBy(w => w.Name, StringComparer.InvariantCulture)
            .ThenBy(w => w.Id)
            .ToList();

        return new WorkDayResponse(
            day.Id,
            day.SiteId,
            day.Date,
            day.Title,
            day.Note,
            items,
            sums.Earned,
            sums.OpenCount,
            workers);
    }

    // Copies the request onto the day. The item list replaces the old one:
    // an item whose Id is an item of this day is updated in place (keeps its Id);
    // any other item is new (an Id of another day's item is ignored, the item gets a new Id);
    // the old items that are not in the request are deleted.
    // The price is taken as sent (also null, „цена?“), for the Owner and the Manager alike (CLAUDE.md "Работни дни");
    // the form suggests it from the site price, then the service's default price.
    private void Apply(WorkDay day, SaveWorkDayRequest request)
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

            // The validator makes Description and Unit required; the fallbacks only keep the compiler happy.
            item ??= new WorkItem { Description = string.Empty };
            item.ServiceId = source.ServiceId;
            item.Description = source.Description?.Trim() ?? string.Empty;
            item.Quantity = source.Quantity;
            item.Unit = source.Unit ?? default;
            item.NeedsReview = source.NeedsReview;
            item.Position = position;
            item.Rate = source.Rate;

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

    // The day's workers become exactly these people with these pays: the links of the others are deleted,
    // the kept ones get the pay as sent, the new ones are added. The pay is a copy (CLAUDE.md "Разход за хора").
    private static void SetWorkers(WorkDay day, IReadOnlyList<ChosenWorker> chosen)
    {
        var wanted = chosen.ToDictionary(c => c.Person.Id);
        day.Workers.RemoveAll(w => !wanted.ContainsKey(w.PersonId));

        foreach (var worker in day.Workers)
        {
            worker.Pay = wanted[worker.PersonId].Pay;
        }

        var current = day.Workers.Select(w => w.PersonId).ToHashSet();
        foreach (var c in chosen.Where(c => !current.Contains(c.Person.Id)))
        {
            day.Workers.Add(new WorkDayWorker { PersonId = c.Person.Id, Person = c.Person, Pay = c.Pay });
        }
    }

    // The people named in the request (the validator lets each person in only once). A person already on the day may
    // stay even when no longer crew; a new one must be crew; an unknown id is an error (WorkDayRules.WorkerError).
    // Errors are a 400 on workers.
    private async Task<Workers> LoadWorkersAsync(
        WorkDay day,
        IReadOnlyList<SaveWorkerRequest> requested,
        CancellationToken cancellationToken)
    {
        if (requested.Count == 0)
        {
            return Workers.None;
        }

        var ids = requested.Select(w => w.PersonId).Distinct().ToList();
        var people = await this.Data.People
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var onTheDay = day.Workers.Select(w => w.PersonId).ToHashSet();

        var messages = ids
            .Select(id => WorkDayRules.WorkerError(
                onTheDay.Contains(id),
                people.TryGetValue(id, out var person) ? person.IsCrew : null))
            .OfType<string>()
            .Distinct()
            .ToArray();
        if (messages.Length > 0)
        {
            return new Workers([], Error.Validation(new Dictionary<string, string[]> { ["workers"] = messages }));
        }

        // Partners may have a pay for the day too („надник“, CLAUDE.md "Разход за хора", рунд 31); it is optional.
        return new Workers([.. requested.Select(w => new ChosenWorker(people[w.PersonId], w.Pay))], null);
    }

    // An unknown service is a 400 on that item (items[i].serviceId); null when all services exist.
    // The service only says where the item came from; its price is not used here.
    private async Task<Error?> UnknownServicesAsync(
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
            return null;
        }

        var known = await this.Data.Services
            .Where(s => serviceIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var errors = items
            .Select((item, index) => (item, index))
            .Where(x => x.item.ServiceId is { } id && !known.Contains(id))
            .ToDictionary(
                x => $"items[{x.index}].serviceId",
                _ => new[] { ServiceRepository.NotFoundMessage });

        return errors.Count > 0 ? Error.Validation(errors) : null;
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

            if (await this.UnknownServicesAsync(Items(request), cancellationToken) is { } error)
            {
                return error;
            }

            // Or a person was deleted.
            var workerIds = day.Workers.Select(w => w.PersonId).ToList();
            if (await this.Data.People.CountAsync(p => workerIds.Contains(p.Id), cancellationToken) < workerIds.Count)
            {
                return Error.Validation(new Dictionary<string, string[]>
                {
                    ["workers"] = [WorkDayRules.WorkerNotFoundMessage],
                });
            }

            throw;
        }

        return ToResponse(day, await this.Data.PartnerPersonIdSetAsync(cancellationToken));
    }

    private async Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
        => await this.Data.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);

    private sealed record ChosenWorker(Person Person, decimal? Pay);

    private sealed record Workers(IReadOnlyList<ChosenWorker> Chosen, Error? Error)
    {
        public static readonly Workers None = new([], null);
    }
}
