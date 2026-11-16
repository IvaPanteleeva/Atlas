namespace Atlas.Application.SiteContacts;

/// <summary>
/// Payload for adding or editing a contact of a site (Owner or Manager). On edit all fields are replaced.
/// Text is trimmed; an empty role, phone or note is stored as null.
/// </summary>
/// <param name="Name">Required, up to 100 characters.</param>
/// <param name="Role">Free text, up to 50 characters (e.g. „собственик“, „съсед“).</param>
/// <param name="Phone">Up to 30 characters.</param>
/// <param name="Note">Up to 500 characters.</param>
public sealed record SaveSiteContactRequest(string Name, string? Role, string? Phone, string? Note);
