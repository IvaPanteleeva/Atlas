namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class CompanyRepository(AtlasDbContext data) : DataRepository<Company>(data), ICompanyRepository
{
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => await this.Data.Companies.AnyAsync(c => c.Id == id, cancellationToken);
}
