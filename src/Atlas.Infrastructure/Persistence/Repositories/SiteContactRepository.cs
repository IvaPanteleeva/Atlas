namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.People;
using Atlas.Application.SiteContacts;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class SiteContactRepository(AtlasDbContext data)
    : DataRepository<SiteContact>(data), ISiteContactRepository
{
    public async Task<Result<IReadOnlyList<SiteContactResponse>>> ListAsync(
        Guid siteId,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        // By name; Id only makes ties stable.
        var contacts = await this.Data.SiteContacts
            .AsNoTracking()
            .Where(c => c.SiteId == siteId)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new SiteContactResponse(c.Id, c.SiteId, c.Name, c.Role, c.Phone, c.Note))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SiteContactResponse>>.SuccessWith(contacts);
    }

    public async Task<Result<SiteContactResponse>> CreateAsync(
        Guid siteId,
        SaveSiteContactRequest request,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        var contact = new SiteContact { SiteId = siteId, Name = string.Empty };
        Apply(contact, request);
        await this.AddAsync(contact, cancellationToken);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // The site was deleted between the check and the save.
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        return ToResponse(contact);
    }

    public async Task<Result<SiteContactResponse>> UpdateAsync(
        Guid siteId,
        Guid id,
        SaveSiteContactRequest request,
        CancellationToken cancellationToken)
    {
        var contact = await this.FindAsync(siteId, id, cancellationToken);
        if (contact is null)
        {
            return Error.NotFound(SiteContactRules.NotFoundMessage);
        }

        Apply(contact, request);
        await this.SaveAsync(cancellationToken);

        return ToResponse(contact);
    }

    public async Task<Result> DeleteAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
    {
        var contact = await this.FindAsync(siteId, id, cancellationToken);
        if (contact is null)
        {
            return Error.NotFound(SiteContactRules.NotFoundMessage);
        }

        this.Delete(contact);
        await this.SaveAsync(cancellationToken);

        return Result.Success;
    }

    private static SiteContactResponse ToResponse(SiteContact contact)
        => new(contact.Id, contact.SiteId, contact.Name, contact.Role, contact.Phone, contact.Note);

    private static void Apply(SiteContact contact, SaveSiteContactRequest request)
    {
        contact.Name = PersonRules.NormalizeName(request.Name);
        contact.Role = PersonRules.NormalizeOptional(request.Role);
        contact.Phone = PersonRules.NormalizeOptional(request.Phone);
        contact.Note = PersonRules.NormalizeOptional(request.Note);
    }

    private async Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
        => await this.Data.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);

    // A contact is only found through its own site: the id of another site's contact is a 404 here.
    private async Task<SiteContact?> FindAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
        => await this.Data.SiteContacts.FirstOrDefaultAsync(c => c.Id == id && c.SiteId == siteId, cancellationToken);
}
