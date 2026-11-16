namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.People;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// Our people (crew) and contacts (CLAUDE.md "Хора и профили"). Owner and Manager read, add and edit them
// (with the notes); only the Owner deletes.
public static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/people")
            .WithTags("People")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetPeople)
            .WithName("GetPeople")
            .WithSummary("List people")
            .WithDescription(
                "Crew and contacts by name, with their days (distinct dates over all sites) and the last one.");

        group.MapPost("/", CreatePerson)
            .WithValidation<SavePersonRequest>()
            .WithName("CreatePerson")
            .WithSummary("Add a person")
            .WithDescription("Adds a crew member or a contact. Text is trimmed; empty phone or note is stored as null.")
            .Produces<PersonResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdatePerson)
            .WithValidation<SavePersonRequest>()
            .WithName("UpdatePerson")
            .WithSummary("Edit a person")
            .WithDescription("Replaces all fields. Taken off the crew, the person stays on their old work days.")
            .Produces<PersonResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeletePerson)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeletePerson")
            .WithSummary("Delete a person")
            .WithDescription("Deletes a person who is on no work day; otherwise 409 (take them off the crew instead).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<PersonResponse>>> GetPeople(
        IPersonRepository people,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await people.ListAsync(cancellationToken));

    private static async Task<IResult> CreatePerson(
        SavePersonRequest request,
        IPersonRepository people,
        CancellationToken cancellationToken)
        => (await people.CreateAsync(request, cancellationToken))
            .ToHttpResult(person => TypedResults.Created($"/api/people/{person.Id}", person));

    private static async Task<IResult> UpdatePerson(
        Guid id,
        SavePersonRequest request,
        IPersonRepository people,
        CancellationToken cancellationToken)
        => (await people.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeletePerson(
        Guid id,
        IPersonRepository people,
        CancellationToken cancellationToken)
        => (await people.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
