namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Contracts;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<Company> needs the Company type,
// and Application does not reference Infrastructure.
public interface ICompanyRepository : IRepository<Company>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
