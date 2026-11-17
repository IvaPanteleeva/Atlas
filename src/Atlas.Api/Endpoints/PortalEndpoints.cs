namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Identity;
using Atlas.Application.Portal;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// The client portal (CLAUDE.md "Достъп за възложителя"): read-only, ClientViewer only (Owner and Manager get 403).
// The company always comes from the signed-in user's claim, never from the request, and the repository filters
// every query by it. A ClientViewer without a company gets 403 (policy Client requires the claim).
public static class PortalEndpoints
{
    public static IEndpointRouteBuilder MapPortalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/portal/sites")
            .WithTags("Portal")
            .RequireAuthorization(Policies.Client);

        group.MapGet("/", ListSites)
            .WithName("GetPortalSites")
            .WithSummary("List the client's sites")
            .WithDescription(
                "The sites of the signed-in client's company at every status, in the order of the internal list, "
                + "with Earned, Paid, Remaining and OpenCount, and „Дължите“ (Owed: positive remainders only), "
                + "Overpaid and OverpaidSiteCount.")
            .Produces<PortalSitesResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetSite)
            .WithName("GetPortalSite")
            .WithSummary("Get one of the client's sites")
            .WithDescription(
                "The site, its sums, the work days (newest first, items with price, amount and flags) and the "
                + "payments (date and amount). 404 for a site of another company or without a company.")
            .Produces<PortalSiteResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<PortalSitesResponse>, ForbidHttpResult>> ListSites(
        ICurrentUser currentUser,
        IPortalRepository portal,
        CancellationToken cancellationToken)
        => currentUser.CompanyId is { } companyId
            ? TypedResults.Ok(await portal.ListAsync(companyId, cancellationToken))
            : TypedResults.Forbid();

    private static async Task<IResult> GetSite(
        Guid id,
        ICurrentUser currentUser,
        IPortalRepository portal,
        CancellationToken cancellationToken)
        => currentUser.CompanyId is { } companyId
            ? (await portal.GetAsync(companyId, id, cancellationToken)).ToHttpResult()
            : TypedResults.Forbid();
}
