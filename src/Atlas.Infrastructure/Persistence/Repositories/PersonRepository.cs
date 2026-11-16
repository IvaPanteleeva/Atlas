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

        // SQL brings each person's dates once; counting them is PersonRules.WorkStats' job.
        var dates = (await this.Data.WorkDayWorkers
                .AsNoTracking()
                .Select(w => new { w.PersonId, w.WorkDay!.Date })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToLookup(w => w.PersonId, w => w.Date);

        return [.. people.Select(p => ToResponse(p, dates[p.Id]))];
    }

    public async Task<Result<PersonResponse>> CreateAsync(SavePersonRequest request, CancellationToken cancellationToken)
    {
        var person = new Person { Name = string.Empty };
        Apply(person, request);
        await this.AddAsync(person, cancellationToken);
        await this.SaveAsync(cancellationToken);

        // A new person has no days yet.
        return ToResponse(person, []);
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

        var dates = await this.Data.WorkDayWorkers
            .AsNoTracking()
            .Where(w => w.PersonId == id)
            .Select(w => w.WorkDay!.Date)
            .ToListAsync(cancellationToken);

        return ToResponse(person, dates);
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
            // The person was put on a work day between the check and the save (the FK is Restrict).
            return Error.Conflict(PersonRules.HasWorkDaysMessage);
        }

        return Result.Success;
    }

    private static void Apply(Person person, SavePersonRequest request)
    {
        person.Name = PersonRules.NormalizeName(request.Name);
        person.Phone = PersonRules.NormalizeOptional(request.Phone);
        person.Note = PersonRules.NormalizeOptional(request.Note);
        person.IsCrew = request.IsCrew;
    }

    private static PersonResponse ToResponse(Person person, IEnumerable<DateOnly> workDates)
    {
        var (dayCount, lastWorkDate) = PersonRules.WorkStats(workDates);

        return new PersonResponse(
            person.Id,
            person.Name,
            person.Phone,
            person.Note,
            person.IsCrew,
            dayCount,
            lastWorkDate);
    }
}
