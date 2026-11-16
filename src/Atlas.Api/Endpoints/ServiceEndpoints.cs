namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Services;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// Services we do (e.g. „Шпакловка“ per м²) with an optional default price. Our people (Owner, Crew)
// read the list (Crew picks from it in a work day); only the Owner adds, edits and deletes
// (CLAUDE.md "Услуги и цени по обект").
public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services")
            .WithTags("Services")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetServices)
            .WithName("GetServices")
            .WithSummary("List services")
            .WithDescription("All services, by name, then unit.");

        group.MapPost("/", CreateService)
            .RequireAuthorization(Policies.Owner)
            .WithValidation<CreateServiceRequest>()
            .WithName("CreateService")
            .WithSummary("Add a service")
            .WithDescription("Adds a service. The name is trimmed; name + unit must be unique (case-insensitive).")
            .Produces<ServiceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateService)
            .RequireAuthorization(Policies.Owner)
            .WithValidation<UpdateServiceRequest>()
            .WithName("UpdateService")
            .WithSummary("Edit a service")
            .WithDescription("Replaces all fields of a service. Work days keep the prices they already copied.")
            .Produces<ServiceResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteService)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteService")
            .WithSummary("Delete a service")
            .WithDescription("Deletes a service that no site has a price for.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ServiceResponse>>> GetServices(
        IServiceRepository services,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await services.ListAsync(cancellationToken));

    private static async Task<IResult> CreateService(
        CreateServiceRequest request,
        IServiceRepository services,
        CancellationToken cancellationToken)
        => (await services.CreateAsync(request, cancellationToken))
            .ToHttpResult(service => TypedResults.Created($"/api/services/{service.Id}", service));

    private static async Task<IResult> UpdateService(
        Guid id,
        UpdateServiceRequest request,
        IServiceRepository services,
        CancellationToken cancellationToken)
        => (await services.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteService(
        Guid id,
        IServiceRepository services,
        CancellationToken cancellationToken)
        => (await services.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
