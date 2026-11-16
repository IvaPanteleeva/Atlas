namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Users;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Users are created through UserManager (password hash, Identity's user name rules),
// which works on the same scoped AtlasDbContext as this repository.
public sealed class UserRepository(AtlasDbContext data, UserManager<AtlasUser> userManager)
    : DataRepository<AtlasUser>(data), IUserRepository
{
    public const string UserNameTakenMessage = "Потребителското име е заето";

    public const string CompanyNotFoundMessage = "Няма такава фирма.";

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (await userManager.FindByNameAsync(request.UserName) is not null)
        {
            return Error.Conflict(UserNameTakenMessage);
        }

        string? companyName = null;
        if (request.CompanyId is { } companyId)
        {
            companyName = await this.Data.Companies
                .Where(c => c.Id == companyId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (companyName is null)
            {
                return FieldError("companyId", CompanyNotFoundMessage);
            }
        }

        // User and role together: a user without a role would be able to sign in but see nothing.
        await using var transaction = await this.Data.Database.BeginTransactionAsync(cancellationToken);

        var user = new AtlasUser { UserName = request.UserName, CompanyId = request.CompanyId };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return ToError(created);
        }

        var inRole = await userManager.AddToRoleAsync(user, request.Role);
        if (!inRole.Succeeded)
        {
            return ToError(inRole);
        }

        await transaction.CommitAsync(cancellationToken);

        return new UserResponse(user.Id, user.UserName, request.Role, user.CompanyId, companyName);
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken)
        => await (
                from user in this.Data.Users
                join userRole in this.Data.UserRoles on user.Id equals userRole.UserId
                join role in this.Data.Roles on userRole.RoleId equals role.Id
                orderby user.UserName
                select new UserResponse(
                    user.Id,
                    user.UserName!,
                    role.Name!,
                    user.CompanyId,
                    user.Company != null ? user.Company.Name : null))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    // A name taken in the meantime is still a conflict; the other Identity rules are field errors
    // (Password* codes on "password", the rest on "userName").
    private static Error ToError(IdentityResult result)
    {
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return Error.Conflict(UserNameTakenMessage);
        }

        var fieldErrors = result.Errors
            .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "userName")
            .ToDictionary(group => group.Key, group => group.Select(e => e.Description).ToArray());

        return Error.Validation(fieldErrors);
    }

    private static Error FieldError(string field, string message)
        => Error.Validation(new Dictionary<string, string[]> { [field] = [message] });
}
