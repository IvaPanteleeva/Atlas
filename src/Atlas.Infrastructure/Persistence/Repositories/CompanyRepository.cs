namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Companies;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class CompanyRepository(AtlasDbContext data) : DataRepository<Company>(data), ICompanyRepository
{
    public const string NotFoundMessage = "Няма такава фирма.";

    public const string NameTakenMessage = "Фирма с това име вече има";

    public const string InUseMessage = "Фирмата се използва";

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => await this.Data.Companies.AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CompanyResponse>> ListAsync(CancellationToken cancellationToken)
        => await this.Data.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponse(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    public async Task<Result<CompanyResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await this.Data.Companies
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CompanyResponse(c.Id, c.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return company is null ? Error.NotFound(NotFoundMessage) : company;
    }

    public async Task<Result<CompanyResponse>> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var name = CompanyRules.NormalizeName(request.Name);

        if (await this.NameTakenAsync(name, exceptId: null, cancellationToken))
        {
            return Error.Conflict(NameTakenMessage);
        }

        var company = new Company { Name = name };
        await this.AddAsync(company, cancellationToken);

        return await this.SaveAsync(company, cancellationToken);
    }

    public async Task<Result<CompanyResponse>> RenameAsync(
        Guid id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var company = await this.Data.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (company is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        var name = CompanyRules.NormalizeName(request.Name);
        if (await this.NameTakenAsync(name, exceptId: id, cancellationToken))
        {
            return Error.Conflict(NameTakenMessage);
        }

        company.Name = name;

        return await this.SaveAsync(company, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await this.Data.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (company is null)
        {
            return Error.NotFound(NotFoundMessage);
        }

        if (await this.IsInUseAsync(id, cancellationToken))
        {
            return Error.Conflict(InUseMessage);
        }

        this.Delete(company);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // Something started pointing to the company after the check (the FKs are Restrict).
            return Error.Conflict(InUseMessage);
        }

        return Result.Success;
    }

    // Everything that points to a company blocks its deletion (CLAUDE.md "Обекти и фирми").
    private async Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await this.Data.Users.AnyAsync(u => u.CompanyId == id, cancellationToken))
        {
            return true;
        }

        // 3.2: add the Sites check here (a company with sites cannot be deleted).
        return false;
    }

    // Case-insensitive: "Женистрой ЕООД" and "ЖЕНИСТРОЙ ЕООД" are the same company (decided in 3.1).
    // Postgres lower() handles Cyrillic. The unique index stays exact, as a last guard against races.
    private async Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var lowered = name.ToLowerInvariant();
        return await this.Data.Companies.AnyAsync(
            c => c.Name.ToLower() == lowered && c.Id != exceptId,
            cancellationToken);
    }

    // The unique index still catches a name taken between the check and the save.
    private async Task<Result<CompanyResponse>> SaveAsync(Company company, CancellationToken cancellationToken)
    {
        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            return Error.Conflict(NameTakenMessage);
        }

        return new CompanyResponse(company.Id, company.Name);
    }
}
