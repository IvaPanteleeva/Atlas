namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Companies;
using Atlas.Application.Contracts;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<Company> needs the Company type,
// and Application does not reference Infrastructure.
public interface ICompanyRepository : IRepository<Company>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    // All companies, ordered by name.
    Task<IReadOnlyList<CompanyResponse>> ListAsync(CancellationToken cancellationToken);

    // NotFound when there is no such company.
    Task<Result<CompanyResponse>> GetAsync(Guid id, CancellationToken cancellationToken);

    // Stores the trimmed name; Conflict when another company already has it.
    Task<Result<CompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken);

    // NotFound, or Conflict when another company already has the (trimmed) name.
    Task<Result<CompanyResponse>> RenameAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken);

    // NotFound, or Conflict while anything still points to the company (users; sites from 3.2).
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
