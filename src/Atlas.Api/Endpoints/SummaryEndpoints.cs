namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Summaries;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// The sums computed on the server (CLAUDE.md "Плащания и суми"): Owner only, at every site status.
// "/api/sites/summaries" does not reach GET /api/sites/{id:guid}: "summaries" is not a guid.
public static class SummaryEndpoints
{
    public static IEndpointRouteBuilder MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites")
            .WithTags("Summaries")
            .RequireAuthorization(Policies.Owner);

        group.MapGet("/{siteId:guid}/summary", GetSiteSummary)
            .WithName("GetSiteSummary")
            .WithSummary("Get the sums of a site")
            .WithDescription(
                "Earned (items with a price, flagged ones included), Paid, Remaining = Earned − Paid (negative = "
                + "overpaid), OpenCount (items without a price or flagged), the number of work days and the last one.")
            .Produces<SiteSummaryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/summaries", GetSiteSummaries)
            .WithName("GetSiteSummaries")
            .WithSummary("Get the sums of all sites")
            .WithDescription(
                "The sums of every site (any status) and the totals for the site list. Owed sums only the positive "
                + "remainders; overpayments are summed apart (Overpaid, OverpaidSiteCount), never netted.");

        return app;
    }

    private static async Task<IResult> GetSiteSummary(
        Guid siteId,
        ISiteSummaryRepository summaries,
        CancellationToken cancellationToken)
        => (await summaries.GetAsync(siteId, cancellationToken)).ToHttpResult();

    private static async Task<Ok<SiteSummariesResponse>> GetSiteSummaries(
        ISiteSummaryRepository summaries,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await summaries.ListAsync(cancellationToken));
}
