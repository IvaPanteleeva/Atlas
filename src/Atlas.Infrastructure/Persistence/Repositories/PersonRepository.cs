namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.People;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class PersonRepository(AtlasDbContext data) : DataRepository<Person>(data), IPersonRepository
{
    public async Task<IReadOnlyList<PersonResponse>> ListAsync(CancellationToken cancellationToken)
    {
        // By name; Id only makes ties (same name) stable. The frontend splits crew and contacts.
        var people = await this.Data.People
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        // SQL brings one row per person-day (date and pay); counting the distinct dates and summing the pay is
        // PersonRules' job.
        var days = (await this.Data.WorkDayWorkers
                .AsNoTracking()
                .Select(w => new { w.PersonId, w.WorkDay!.Date, w.Pay })
                .ToListAsync(cancellationToken))
            .ToLookup(w => w.PersonId, w => new PersonDay(w.Date, w.Pay));

        var links = await this.LinksAsync(null, cancellationToken);

        return [.. people.Select(p => ToResponse(p, days[p.Id], links.GetValueOrDefault(p.Id)))];
    }

    public async Task<Result<PersonResponse>> CreateAsync(SavePersonRequest request, CancellationToken cancellationToken)
    {
        var person = new Person { Name = string.Empty };
        Apply(person, request);
        await this.AddAsync(person, cancellationToken);
        await this.SaveAsync(cancellationToken);

        // A new person has no days and no profile yet.
        return ToResponse(person, [], null);
    }

    public async Task<Result<PersonResponse>> UpdateAsync(
        Guid id,
        SavePersonRequest request,
        CancellationToken cancellationToken)
    {
        var person = await this.Data.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return Error.NotFound(PersonRules.NotFoundMessage);
        }

        // Taking someone off the crew keeps them on their old days (CLAUDE.md "Хора и профили").
        Apply(person, request);
        await this.SaveAsync(cancellationToken);

        var days = await this.Data.WorkDayWorkers
            .AsNoTracking()
            .Where(w => w.PersonId == id)
            .Select(w => new PersonDay(w.WorkDay!.Date, w.Pay))
            .ToListAsync(cancellationToken);

        var links = await this.LinksAsync(id, cancellationToken);

        return ToResponse(person, days, links.GetValueOrDefault(id));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var person = await this.Data.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return Error.NotFound(PersonRules.NotFoundMessage);
        }

        // Someone with work days stays (the days are their pay history); take them off the crew instead.
        if (await this.Data.WorkDayWorkers.AnyAsync(w => w.PersonId == id, cancellationToken))
        {
            return Error.Conflict(PersonRules.HasWorkDaysMessage);
        }

        // A person linked to a login profile is unlinked there first (PUT /api/users/{id}/person).
        if (await this.Data.Users.AnyAsync(u => u.PersonId == id, cancellationToken))
        {
            return Error.Conflict(PersonRules.LinkedToUserMessage);
        }

        this.Delete(person);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // The person was put on a work day or linked to a profile between the checks and the save
            // (both FKs are Restrict).
            this.Data.ChangeTracker.Clear();
            return Error.Conflict(await this.Data.WorkDayWorkers.AnyAsync(w => w.PersonId == id, cancellationToken)
                ? PersonRules.HasWorkDaysMessage
                : PersonRules.LinkedToUserMessage);
        }

        return Result.Success;
    }

    private static void Apply(Person person, SavePersonRequest request)
    {
        person.Name = PersonRules.NormalizeName(request.Name);
        person.Phone = PersonRules.NormalizeOptional(request.Phone);
        person.Note = PersonRules.NormalizeOptional(request.Note);
        person.IsCrew = request.IsCrew;

        // Only new days are suggested the new rate; the days already saved keep their pay.
        person.DailyRate = request.DailyRate;
    }

    private static PersonResponse ToResponse(Person person, IEnumerable<PersonDay> days, Link? link)
    {
        var list = days as IReadOnlyCollection<PersonDay> ?? [.. days];
        var (dayCount, lastWorkDate) = PersonRules.WorkStats(list.Select(d => d.Date));

        return new PersonResponse(
            person.Id,
            person.Name,
            person.Phone,
            person.Note,
            person.IsCrew,
            dayCount,
            lastWorkDate,
            person.DailyRate,
            PersonRules.Earned(list.Select(d => d.Pay)),
            link?.IsPartner ?? false,
            link?.UserName);
    }

    // The login profile linked to each person (all, or one), and whether its role makes the person a partner.
    private async Task<Dictionary<Guid, Link>> LinksAsync(Guid? personId, CancellationToken cancellationToken)
    {
        var partners = await this.Data.PartnerPersonIdSetAsync(cancellationToken);

        var users = await this.Data.Users
            .AsNoTracking()
            .Where(u => u.PersonId != null && (personId == null || u.PersonId == personId))
            .Select(u => new { PersonId = u.PersonId!.Value, u.UserName })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.PersonId, u => new Link(u.UserName!, partners.Contains(u.PersonId)));
    }

    private sealed record Link(string UserName, bool IsPartner);

    // One day the person worked: its date and the pay for it.
    private sealed record PersonDay(DateOnly Date, decimal? Pay);
}
