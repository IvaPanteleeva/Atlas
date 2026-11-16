namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Users;
using Atlas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

// Owner only: list users and create Owner (partner), Crew and ClientViewer accounts.
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
                + "Owner and Crew must have none.")
            .Produces<UserResponse>(StatusCodes.Status201Created)
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
}
