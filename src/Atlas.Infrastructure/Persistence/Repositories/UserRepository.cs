namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Identity;
using Atlas.Application.People;
using Atlas.Application.Users;
using Atlas.Application.WorkDays;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// Users are created through UserManager (password hash, Identity's user name rules),
// which works on the same scoped AtlasDbContext as this repository.
public sealed class UserRepository(AtlasDbContext data, UserManager<AtlasUser> userManager, ICurrentUser currentUser)
    : DataRepository<AtlasUser>(data), IUserRepository
{
    public const string NotFoundMessage = "Няма такъв профил.";

    public const string CannotDeleteSelfMessage = "Не можеш да изтриеш собствения си профил.";

    public const string LastOwnerMessage = "Трябва да остане поне един Owner.";

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

        string? personName = null;
        if (request.PersonId is { } personId)
        {
            var person = await this.CheckPersonAsync(personId, exceptUserId: null, cancellationToken);
            if (!person.Succeeded)
            {
                return person.Error;
            }

            personName = person.Data;
        }

        // User and role together: a user without a role would be able to sign in but see nothing.
        await using var transaction = await this.Data.Database.BeginTransactionAsync(cancellationToken);

        var user = new AtlasUser { UserName = request.UserName, CompanyId = request.CompanyId, PersonId = request.PersonId };

        IdentityResult created;
        try
        {
            created = await userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException ex) when (PersonRaceError(ex) is { } error)
        {
            return error;
        }

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

        return new UserResponse(user.Id, user.UserName, request.Role, user.CompanyId, companyName, user.PersonId, personName);
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
                    user.Company != null ? user.Company.Name : null,
                    user.PersonId,
                    user.Person != null ? user.Person.Name : null))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Result<UserResponse>> LinkPersonAsync(
        Guid id,
        LinkPersonRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        if (request.PersonId is { } personId)
        {
            if (await userManager.IsInRoleAsync(user, Roles.ClientViewer))
            {
                return FieldError("personId", UserRules.ClientViewerPersonMessage);
            }

            var person = await this.CheckPersonAsync(personId, exceptUserId: id, cancellationToken);
            if (!person.Succeeded)
            {
                return person.Error;
            }
        }

        // Unlinking or relinking changes who is a partner from now on; the pays stored on the days stay.
        user.PersonId = request.PersonId;
        IdentityResult updated;
        try
        {
            updated = await userManager.UpdateAsync(user);
        }
        catch (DbUpdateException ex) when (PersonRaceError(ex) is { } error)
        {
            return error;
        }

        if (!updated.Succeeded)
        {
            return ToError(updated);
        }

        return (await this.ListAsync(cancellationToken)).Single(u => u.Id == id);
    }

    public async Task<Result> SetPasswordAsync(
        Guid id,
        SetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        // No reset tokens (no token providers are registered, and the Owner sets the password directly):
        // remove + add in one transaction, so a password the Identity rules reject leaves the old one in place.
        // Both calls change the security stamp, so the user's other sessions stop working when their cookie is
        // checked again (SecurityStampValidator).
        await using var transaction = await this.Data.Database.BeginTransactionAsync(cancellationToken);

        var removed = await userManager.RemovePasswordAsync(user);
        if (!removed.Succeeded)
        {
            return ToError(removed);
        }

        var added = await userManager.AddPasswordAsync(user, request.Password);
        if (!added.Succeeded)
        {
            return ToError(added);
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        if (id == currentUser.UserId)
        {
            return Error.Conflict(CannotDeleteSelfMessage);
        }

        // Someone must still be able to manage the users (CLAUDE.md "Хора и профили").
        if (await userManager.IsInRoleAsync(user, Roles.Owner)
            && (await userManager.GetUsersInRoleAsync(Roles.Owner)).Count <= 1)
        {
            return Error.Conflict(LastOwnerMessage);
        }

        // The roles and other Identity rows go with the user (cascade).
        var deleted = await userManager.DeleteAsync(user);
        if (!deleted.Succeeded)
        {
            throw new InvalidOperationException(
                $"Deleting user {id} failed: {string.Join(", ", deleted.Errors.Select(e => e.Code))}");
        }

        return Result.Success;
    }

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

    // Between the person checks and the save: the unique index on PersonId (linked to another profile meanwhile)
    // or the FK (the person was deleted). Other database errors are not handled here.
    private static Error? PersonRaceError(DbUpdateException ex)
        => ex.InnerException switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: var name }
                when name?.Contains("PersonId", StringComparison.Ordinal) == true
                => Error.Conflict(UserRules.PersonTakenMessage),
            PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: var name }
                when name?.Contains("People", StringComparison.Ordinal) == true
                => FieldError("personId", PersonRules.NotFoundMessage),
            _ => null,
        };

    // The person exists, is crew and is not linked to another profile. Returns the person's name.
    private async Task<Result<string>> CheckPersonAsync(
        Guid personId,
        Guid? exceptUserId,
        CancellationToken cancellationToken)
    {
        var person = await this.Data.People
            .AsNoTracking()
            .Where(p => p.Id == personId)
            .Select(p => new { p.Name, p.IsCrew })
            .FirstOrDefaultAsync(cancellationToken);
        if (person is null)
        {
            return FieldError("personId", PersonRules.NotFoundMessage);
        }

        if (!person.IsCrew)
        {
            return FieldError("personId", WorkDayRules.WorkerNotCrewMessage);
        }

        if (await this.Data.Users.AnyAsync(u => u.PersonId == personId && u.Id != exceptUserId, cancellationToken))
        {
            return Error.Conflict(UserRules.PersonTakenMessage);
        }

        return person.Name;
    }

    private static Error FieldError(string field, string message)
        => Error.Validation(new Dictionary<string, string[]> { [field] = [message] });
}
