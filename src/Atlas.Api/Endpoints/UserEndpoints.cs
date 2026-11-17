namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Users;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// Owner only: list users, create Owner, Manager and ClientViewer accounts, link a profile to a crew member,
// set a new password, delete.
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(Policies.Owner);

        group.MapGet("/", GetUsers)
            .WithName("GetUsers")
            .WithSummary("List users")
            .WithDescription("All users with their role and company, ordered by user name.");

        group.MapPost("/", CreateUser)
            .WithValidation<CreateUserRequest>()
            .WithName("CreateUser")
            .WithSummary("Create a user")
            .WithDescription("Creates a user with one role. ClientViewer needs an existing company; "
                + "Owner and Manager must have none.")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/password", SetPassword)
            .WithValidation<SetPasswordRequest>()
            .WithName("SetUserPassword")
            .WithSummary("Set a new password")
            .WithDescription("Replaces the user's password (at least 8 characters). The user's other sessions end.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/person", LinkPerson)
            .WithName("LinkUserToPerson")
            .WithSummary("Link a user to a crew member")
            .WithDescription(
                "Links the profile to a crew member, or unlinks it with personId null. A crew member linked to an Owner "
                + "or Manager profile is a partner („съдружник“): on the work days without a pay.")
            .Produces<UserResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .WithSummary("Delete a user")
            .WithDescription("Deletes a user. Not your own user, and not the last Owner (409).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<UserResponse>>> GetUsers(
        IUserRepository users,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await users.ListAsync(cancellationToken));

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        IUserRepository users,
        CancellationToken cancellationToken)
        => (await users.CreateAsync(request, cancellationToken))
            .ToHttpResult(user => TypedResults.Created($"/api/users/{user.Id}", user));

    private static async Task<IResult> SetPassword(
        Guid id,
        SetPasswordRequest request,
        IUserRepository users,
        CancellationToken cancellationToken)
        => (await users.SetPasswordAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> LinkPerson(
        Guid id,
        LinkPersonRequest request,
        IUserRepository users,
        CancellationToken cancellationToken)
        => (await users.LinkPersonAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteUser(
        Guid id,
        IUserRepository users,
        CancellationToken cancellationToken)
        => (await users.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
