namespace Atlas.Application.SiteContacts;

/// <summary>A contact of a site (owner of the flat, neighbour, ...). Not shown in the client portal.</summary>
/// <param name="Id">Contact id.</param>
/// <param name="SiteId">The site.</param>
/// <param name="Name">Name.</param>
/// <param name="Role">Free text role, or null.</param>
/// <param name="Phone">Phone, or null.</param>
/// <param name="Note">Note, or null.</param>
public sealed record SiteContactResponse(Guid Id, Guid SiteId, string Name, string? Role, string? Phone, string? Note);
