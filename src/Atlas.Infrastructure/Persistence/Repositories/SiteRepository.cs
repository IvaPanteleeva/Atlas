namespace Atlas.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Atlas.Application.Common;
using Atlas.Application.Sites;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class SiteRepository(AtlasDbContext data) : DataRepository<Site>(data), ISiteRepository
{
    public const string NotFoundMessage = "Няма такъв обект.";

    public const string CompanyNotFoundMessage = "Няма такава фирма.";

    public const string HasRecordsMessage = "Обектът има записи";

    private static readonly Expression<Func<Site, SiteResponse>> ToResponse = s => new SiteResponse(
        s.Id,
        s.Name,
        s.Status,
        s.CompanyId,
        s.Company == null ? null : s.Company.Name,
        s.EndCustomer,
        s.Address,
        s.MapUrl,
        s.Note);

    // Ordering is done in SQL (a CASE on the status column): current sites first, then leads, then finished
    // (CLAUDE.md "Обекти и фирми"). Later the current ones go by their last work day.
    public async Task<IReadOnlyList<SiteListResponse>> ListAsync(CancellationToken cancellationToken)
        => await this.Data.Sites
            .AsNoTracking()
            .OrderBy(s => s.Status == SiteStatus.Active ? 0 : s.Status == SiteStatus.Lead ? 1 : 2)
            .ThenBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Select(s => new SiteListResponse(
                s.Id,
                s.Name,
                s.Status,
                s.Company == null ? null : s.Company.Name,
                s.EndCustomer,
                s.Address))
            .ToListAsync(cancellationToken);

    public async Task<Result<SiteResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var site = await this.Data.Sites
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return site is null ? Error.NotFound(NotFoundMessage) : site;
    }

    public async Task<Result<SiteResponse>> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CompanyExistsAsync(request.CompanyId, cancellationToken))
        {
            return CompanyNotFound();
        }

        var site = new Site
        {
            Name = SiteRules.NormalizeName(request.Name),
            Status = request.Status ?? SiteStatus.Lead,
        };
        Apply(site, request.CompanyId, request.EndCustomer, request.Address, request.MapUrl, request.Note);

        await this.AddAsync(site, cancellationToken);

        return await this.SaveAndGetAsync(site.Id, cancellationToken);
    }

    public async Task<Result<SiteResponse>> UpdateAsync(
        Guid id,
        UpdateSiteRequest request,
        CancellationToken cancellationToken)
    {
        var site = await this.Data.Sites.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (site is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        if (!await this.CompanyExistsAsync(request.CompanyId, cancellationToken))
        {
            return CompanyNotFound();
        }

        site.Name = SiteRules.NormalizeName(request.Name);

        // The validator makes Status required on update; the fallback only keeps the compiler happy.
        site.Status = request.Status ?? site.Status;
        Apply(site, request.CompanyId, request.EndCustomer, request.Address, request.MapUrl, request.Note);

        return await this.SaveAndGetAsync(site.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var site = await this.Data.Sites.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (site is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        // A site with records cannot be deleted; it is set to Done instead (CLAUDE.md "Обекти и фирми").
        // Feature 6: client payments join this check.
        if (await this.Data.WorkDays.AnyAsync(d => d.SiteId == id, cancellationToken))
        {
            return Error.Conflict(HasRecordsMessage);
        }

        this.Delete(site);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // A work day was added between the check and the save (the FK is Restrict).
            return Error.Conflict(HasRecordsMessage);
        }

        return Result.Success;
    }

    private static void Apply(
        Site site,
        Guid? companyId,
        string? endCustomer,
        string? address,
        string? mapUrl,
        string? note)
    {
        site.CompanyId = companyId;
        site.EndCustomer = SiteRules.NormalizeOptional(endCustomer);
        site.Address = SiteRules.NormalizeOptional(address);
        site.MapUrl = SiteRules.NormalizeOptional(mapUrl);
        site.Note = SiteRules.NormalizeOptional(note);
    }

    private static Error CompanyNotFound()
        => Error.Validation(new Dictionary<string, string[]> { ["companyId"] = [CompanyNotFoundMessage] });

    private async Task<bool> CompanyExistsAsync(Guid? companyId, CancellationToken cancellationToken)
        => companyId is not { } id
            || await this.Data.Companies.AnyAsync(c => c.Id == id, cancellationToken);

    // The FK still catches a company deleted between the check and the save.
    // Reads the site back so the response has the company name.
    private async Task<Result<SiteResponse>> SaveAndGetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            return CompanyNotFound();
        }

        return await this.GetAsync(id, cancellationToken);
    }
}
