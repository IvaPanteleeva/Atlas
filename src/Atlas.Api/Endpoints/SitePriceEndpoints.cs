namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.SitePrices;
using Atlas.Infrastructure.Persistence.Repositories;

// Prices agreed for services on one site. Our people (Owner, Crew) read them; only the Owner
// adds, changes and deletes (CLAUDE.md "Услуги и цени по обект").
public static class SitePriceEndpoints
{
    public static IEndpointRouteBuilder MapSitePriceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites/{siteId:guid}/prices")
            .WithTags("SitePrices")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetSitePrices)
            .WithName("GetSitePrices")
            .WithSummary("List the prices of a site")
            .WithDescription("The prices agreed for the site, by service name, with the service's default price.")
            .Produces<IReadOnlyList<SitePriceResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSitePrice)
            .RequireAuthorization(Policies.Owner)
            .WithValidation<CreateSitePriceRequest>()
            .WithName("CreateSitePrice")
            .WithSummary("Add a price for a site")
            .WithDescription("Adds the price of a service on the site. One price per service and site.")
            .Produces<SitePriceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{priceId:guid}", UpdateSitePrice)
            .RequireAuthorization(Policies.Owner)
            .WithValidation<UpdateSitePriceRequest>()
            .WithName("UpdateSitePrice")
            .WithSummary("Change a site price")
            .WithDescription("Changes the price. Work days keep the prices they already copied.")
            .Produces<SitePriceResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{priceId:guid}", DeleteSitePrice)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteSitePrice")
            .WithSummary("Delete a site price")
            .WithDescription("Deletes the price; the site then uses the service's default price.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetSitePrices(
        Guid siteId,
        ISitePriceRepository prices,
        CancellationToken cancellationToken)
        => (await prices.ListAsync(siteId, cancellationToken)).ToHttpResult();

    private static async Task<IResult> CreateSitePrice(
        Guid siteId,
        CreateSitePriceRequest request,
        ISitePriceRepository prices,
        CancellationToken cancellationToken)
        => (await prices.CreateAsync(siteId, request, cancellationToken))
            .ToHttpResult(price => TypedResults.Created($"/api/sites/{siteId}/prices/{price.Id}", price));

    private static async Task<IResult> UpdateSitePrice(
        Guid siteId,
        Guid priceId,
        UpdateSitePriceRequest request,
        ISitePriceRepository prices,
        CancellationToken cancellationToken)
        => (await prices.UpdateAsync(siteId, priceId, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteSitePrice(
        Guid siteId,
        Guid priceId,
        ISitePriceRepository prices,
        CancellationToken cancellationToken)
        => (await prices.DeleteAsync(siteId, priceId, cancellationToken)).ToHttpResult();
}
