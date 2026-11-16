namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Companies;
using Atlas.Application.Identity;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// Companies that give us work. Owner and Manager read, add and rename them; only the Owner deletes.
public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .WithTags("Companies")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetCompanies)
            .WithName("GetCompanies")
            .WithSummary("List companies")
            .WithDescription("All companies, ordered by name.");

        group.MapPost("/", CreateCompany)
            .WithValidation<CreateCompanyRequest>()
            .WithName("CreateCompany")
            .WithSummary("Add a company")
            .WithDescription("Adds a company. The name is trimmed and must be unique.")
            .Produces<CompanyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", RenameCompany)
            .WithValidation<UpdateCompanyRequest>()
            .WithName("RenameCompany")
            .WithSummary("Rename a company")
            .WithDescription("Renames a company. The name is trimmed and must be unique.")
            .Produces<CompanyResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteCompany)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteCompany")
            .WithSummary("Delete a company")
            .WithDescription("Deletes a company that nothing uses (no users and no sites).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<CompanyResponse>>> GetCompanies(
        ICompanyRepository companies,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await companies.ListAsync(cancellationToken));

    private static async Task<IResult> CreateCompany(
        CreateCompanyRequest request,
        ICompanyRepository companies,
        CancellationToken cancellationToken)
        => (await companies.CreateAsync(request, cancellationToken))
            .ToHttpResult(company => TypedResults.Created($"/api/companies/{company.Id}", company));

    private static async Task<IResult> RenameCompany(
        Guid id,
        UpdateCompanyRequest request,
        ICompanyRepository companies,
        CancellationToken cancellationToken)
        => (await companies.RenameAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteCompany(
        Guid id,
        ICompanyRepository companies,
        CancellationToken cancellationToken)
        => (await companies.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
