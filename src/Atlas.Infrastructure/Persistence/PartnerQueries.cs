namespace Atlas.Infrastructure.Persistence;

using Atlas.Application.Identity;
using Microsoft.EntityFrameworkCore;

// Partners („съдружници“, CLAUDE.md "Разход за хора"): crew members linked to a login profile with role Owner or
// Manager. Not stored on the person: it is read from the current links and roles, so unlinking a profile or
// changing its role changes it from then on.
public static class PartnerQueries
{
    public static IQueryable<Guid> PartnerPersonIds(this AtlasDbContext data)
        => from user in data.Users
           join userRole in data.UserRoles on user.Id equals userRole.UserId
           join role in data.Roles on userRole.RoleId equals role.Id
           where user.PersonId != null && Roles.Partner.Contains(role.Name!)
           select user.PersonId!.Value;

    public static async Task<HashSet<Guid>> PartnerPersonIdSetAsync(
        this AtlasDbContext data,
        CancellationToken cancellationToken)
        => [.. await data.PartnerPersonIds().Distinct().ToListAsync(cancellationToken)];
}
