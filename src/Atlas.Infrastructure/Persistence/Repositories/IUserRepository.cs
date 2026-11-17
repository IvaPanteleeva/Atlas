namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Users;
using Atlas.Infrastructure.Entities;

public interface IUserRepository : IRepository<AtlasUser>
{
    // Validation (company, person, Identity rules) or Conflict (taken name, person linked to another profile);
    // otherwise the new user with its role.
    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    // All users with their role and company, ordered by user name.
    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken);

    // Sets a new password (Identity rules; Validation on password). The security stamp changes, so the user's
    // other sessions end. NotFound when there is no such user.
    Task<Result> SetPasswordAsync(Guid id, SetPasswordRequest request, CancellationToken cancellationToken);

    // Links the profile to a crew member (a partner when the role is Owner or Manager) or unlinks it (null).
    // NotFound; Validation on personId (unknown, not crew, ClientViewer profile); Conflict when the person is
    // already linked to another profile.
    Task<Result<UserResponse>> LinkPersonAsync(Guid id, LinkPersonRequest request, CancellationToken cancellationToken);

    // The person linked to the profile, or null (also for an unknown user). Read from the database, not from the
    // cookie: the link may change while the user is signed in.
    Task<Guid?> GetPersonIdAsync(Guid id, CancellationToken cancellationToken);

    // NotFound; Conflict for the caller's own user or for the last Owner.
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
