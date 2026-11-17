namespace Atlas.Application.People;

/// <summary>A person: one of our crew or a contact, with the days they worked.</summary>
/// <param name="Id">Person id.</param>
/// <param name="Name">Name.</param>
/// <param name="Phone">Phone, or null.</param>
/// <param name="Note">Internal note (Owner and Manager); never shown in the client portal.</param>
/// <param name="IsCrew">Works with us (crew); otherwise a contact.</param>
/// <param name="DayCount">Distinct dates the person worked on, over all sites (two sites on one date = 1 day).</param>
/// <param name="LastWorkDate">The latest of those dates, or null without work days.</param>
/// <param name="DailyRate">Daily rate in EUR, or null.</param>
/// <param name="Earned">„Изкарал“: sum of the person's pay over all their work days (days without a pay add nothing).</param>
/// <param name="IsPartner">Partner („съдружник“): linked to a login profile with role Owner or Manager. A partner's pay
/// for the day is optional (рунд 31); the site's „Остава за нас“ is the partners' profit.</param>
/// <param name="LinkedUserName">The login profile linked to this person, or null.</param>
public sealed record PersonResponse(
    Guid Id,
    string Name,
    string? Phone,
    string? Note,
    bool IsCrew,
    int DayCount,
    DateOnly? LastWorkDate,
    decimal? DailyRate,
    decimal Earned,
    bool IsPartner,
    string? LinkedUserName);
