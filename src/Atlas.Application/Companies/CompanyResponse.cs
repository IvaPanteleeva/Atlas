namespace Atlas.Application.Companies;

/// <summary>A company that gives us work (e.g. ЖЕНИСТРОЙ ЕООД).</summary>
/// <param name="Id">Company id.</param>
/// <param name="Name">Unique company name.</param>
public sealed record CompanyResponse(Guid Id, string Name);
