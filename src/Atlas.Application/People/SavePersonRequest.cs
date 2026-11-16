namespace Atlas.Application.People;

/// <summary>
/// Payload for adding or editing a person (Owner only). On edit all fields are replaced.
/// Text is trimmed; an empty phone or note is stored as null.
/// </summary>
/// <param name="Name">Required, up to 100 characters; the same name may be used twice.</param>
/// <param name="Phone">Up to 30 characters, free text (e.g. "0888 123 456").</param>
/// <param name="Note">Internal note, up to 500 characters; only the Owner sees it.</param>
/// <param name="IsCrew">Works with us (crew); otherwise a contact.</param>
public sealed record SavePersonRequest(string Name, string? Phone, string? Note, bool IsCrew);
