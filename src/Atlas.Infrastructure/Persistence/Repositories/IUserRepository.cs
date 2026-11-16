namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Users;
using Atlas.Infrastructure.Entities;

public interface IUserRepository : IRepository<AtlasUser>
{
    // Validation (company, Identity rules) or Conflict (taken name); otherwise the new user with its role.
    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    // All users with their role and company, ordered by user name.
    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken);
}
