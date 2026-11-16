namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.SiteContacts;
using Atlas.Infrastructure.Persistence.Repositories;

// Contacts of one site (CLAUDE.md "Контакти на обекта"): Owner and Manager read, add and edit them;
// only the Owner deletes. They go with the site when it is deleted.
public static class SiteContactEndpoints
{
    public static IEndpointRouteBuilder MapSiteContactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites/{siteId:guid}/contacts")
            .WithTags("SiteContacts")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetSiteContacts)
            .WithName("GetSiteContacts")
            .WithSummary("List the contacts of a site")
            .WithDescription("The contacts of the site, by name.")
            .Produces<IReadOnlyList<SiteContactResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSiteContact)
            .WithValidation<SaveSiteContactRequest>()
            .WithName("CreateSiteContact")
            .WithSummary("Add a contact to a site")
            .WithDescription("Adds a contact (owner of the flat, neighbour, ...). Text is trimmed; empty is stored as null.")
            .Produces<SiteContactResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateSiteContact)
            .WithValidation<SaveSiteContactRequest>()
            .WithName("UpdateSiteContact")
            .WithSummary("Edit a contact of a site")
            .WithDescription("Replaces all fields of the contact.")
            .Produces<SiteContactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteSiteContact)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeleteSiteContact")
            .WithSummary("Delete a contact of a site")
            .WithDescription("Deletes the contact (Owner only).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetSiteContacts(
        Guid siteId,
        ISiteContactRepository contacts,
        CancellationToken cancellationToken)
        => (await contacts.ListAsync(siteId, cancellationToken)).ToHttpResult();

    private static async Task<IResult> CreateSiteContact(
        Guid siteId,
        SaveSiteContactRequest request,
        ISiteContactRepository contacts,
        CancellationToken cancellationToken)
        => (await contacts.CreateAsync(siteId, request, cancellationToken))
            .ToHttpResult(contact => TypedResults.Created($"/api/sites/{siteId}/contacts/{contact.Id}", contact));

    private static async Task<IResult> UpdateSiteContact(
        Guid siteId,
        Guid id,
        SaveSiteContactRequest request,
        ISiteContactRepository contacts,
        CancellationToken cancellationToken)
        => (await contacts.UpdateAsync(siteId, id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeleteSiteContact(
        Guid siteId,
        Guid id,
        ISiteContactRepository contacts,
        CancellationToken cancellationToken)
        => (await contacts.DeleteAsync(siteId, id, cancellationToken)).ToHttpResult();
}
