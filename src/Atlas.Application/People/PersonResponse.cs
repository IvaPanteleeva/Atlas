namespace Atlas.Application.People;

/// <summary>A person: one of our crew or a contact, with the days they worked.</summary>
/// <param name="Id">Person id.</param>
/// <param name="Name">Name.</param>
/// <param name="Phone">Phone, or null.</param>
/// <param name="Note">Internal note (Owner and Manager); never shown in the client portal.</param>
/// <param name="IsCrew">Works with us (crew); otherwise a contact.</param>
/// <param name="DayCount">Distinct dates the person worked on, over all sites (two sites on one date = 1 day).</param>
/// <param name="LastWorkDate">The latest of those dates, or null without work days.</param>
public sealed record PersonResponse(
    Guid Id,
    string Name,
    string? Phone,
    string? Note,
    bool IsCrew,
    int DayCount,
    DateOnly? LastWorkDate);
