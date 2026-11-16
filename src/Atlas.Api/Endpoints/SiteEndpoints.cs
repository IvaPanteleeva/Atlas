namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Sites;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// Repair sites. Our people (Owner, Crew) see, add and edit them; only the Owner deletes
// (CLAUDE.md "Обекти и фирми"). The client company sees its sites through /portal/... (feature 9).
public static class SiteEndpoints
{
    public static IEndpointRouteBuilder MapSiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites")
            .WithTags("Sites")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetSites)
            .WithName("GetSites")
            .WithSummary("List sites")
            .WithDescription("All sites: Active, then Lead, then Done; by name inside each group. Without the note.");

        group.MapGet("/{id:guid}", GetSite)
            .WithName("GetSite")
            .WithSummary("Get a site")
            .WithDescription("One site with all its data, including the internal note.")
            .Produces<SiteResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSite)
            .WithValidation<CreateSiteRequest>()
            .WithName("CreateSite")
            .WithSummary("Add a site")
            .WithDescription("Adds a site. Text is trimmed, empty optional text is stored as null, a missing status is Lead.")
            .Produces<SiteResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateSite)
            .WithValidation<UpdateSiteRequest>()
            .WithName("UpdateSite")
            .WithSummary("Edit a site")
            .WithDescription("Replaces all fields of a site; the status is required.")
            .Produces<SiteResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteSite)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteSite")
            .WithSummary("Delete a site")
            .WithDescription("Deletes a site (Owner only). A site with work days cannot be deleted (409); set it to Done.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<SiteListResponse>>> GetSites(
        ISiteRepository sites,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await sites.ListAsync(cancellationToken));

    private static async Task<IResult> GetSite(
        Guid id,
        ISiteRepository sites,
        CancellationToken cancellationToken)
        => (await sites.GetAsync(id, cancellationToken)).ToHttpResult();

    private static async Task<IResult> CreateSite(
        CreateSiteRequest request,
        ISiteRepository sites,
        CancellationToken cancellationToken)
        => (await sites.CreateAsync(request, cancellationToken))
            .ToHttpResult(site => TypedResults.Created($"/api/sites/{site.Id}", site));

    private static async Task<IResult> UpdateSite(
        Guid id,
        UpdateSiteRequest request,
        ISiteRepository sites,
        CancellationToken cancellationToken)
        => (await sites.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteSite(
        Guid id,
        ISiteRepository sites,
        CancellationToken cancellationToken)
        => (await sites.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
