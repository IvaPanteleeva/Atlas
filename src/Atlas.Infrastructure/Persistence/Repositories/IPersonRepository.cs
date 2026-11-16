namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.People;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<Person> needs the Person type.
public interface IPersonRepository : IRepository<Person>
{
    // All people (crew and contacts) by name, with their days over all sites.
    Task<IReadOnlyList<PersonResponse>> ListAsync(CancellationToken cancellationToken);

    // Stores normalized text (trimmed, empty → null).
    Task<Result<PersonResponse>> CreateAsync(SavePersonRequest request, CancellationToken cancellationToken);

    // Replaces all fields. NotFound when there is no such person.
    Task<Result<PersonResponse>> UpdateAsync(Guid id, SavePersonRequest request, CancellationToken cancellationToken);

    // NotFound, or Conflict „Човекът има работни дни“ while the person is on any work day.
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
