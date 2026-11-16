namespace Atlas.Application.Sites;

/// <summary>A site with all its data (internal API, includes the note).</summary>
/// <param name="Id">Site id.</param>
/// <param name="Name">Site name (not unique).</param>
/// <param name="Status">Lead, Active or Done.</param>
/// <param name="CompanyId">The company that gives us the work, if any.</param>
/// <param name="CompanyName">Name of that company.</param>
/// <param name="EndCustomer">The end customer, free text (e.g. „Професорът“).</param>
/// <param name="Address">Address.</param>
/// <param name="MapUrl">http/https link to a map.</param>
/// <param name="Note">Internal note; never shown to the client company.</param>
public sealed record SiteResponse(
    Guid Id,
    string Name,
    SiteStatus Status,
    Guid? CompanyId,
    string? CompanyName,
    string? EndCustomer,
    string? Address,
    string? MapUrl,
    string? Note);
