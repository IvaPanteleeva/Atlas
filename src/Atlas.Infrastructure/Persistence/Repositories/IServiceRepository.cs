namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Services;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<Service> needs the Service type.
public interface IServiceRepository : IRepository<Service>
{
    // All services, by name, then unit.
    Task<IReadOnlyList<ServiceResponse>> ListAsync(CancellationToken cancellationToken);

    // NotFound when there is no such service.
    Task<Result<ServiceResponse>> GetAsync(Guid id, CancellationToken cancellationToken);

    // Stores the trimmed name. Conflict when the same name (case-insensitive) with the same unit exists.
    Task<Result<ServiceResponse>> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken);

    // Replaces all fields. NotFound, or Conflict as when adding.
    Task<Result<ServiceResponse>> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken);

    // NotFound, or Conflict „Услугата има цени в N обекта“ when sites have prices for it.
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
