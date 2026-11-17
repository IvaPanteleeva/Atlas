namespace Atlas.Application.WorkDays;

/// <summary>A person who worked on a work day.</summary>
/// <param name="Id">Person id.</param>
/// <param name="Name">The person's current name.</param>
/// <param name="Pay">What the person gets for this day in EUR, or null („за уточняване“).</param>
/// <param name="IsPartner">The person is now a partner (linked to an Owner or Manager profile): the pay is optional.</param>
public sealed record WorkerResponse(Guid Id, string Name, decimal? Pay, bool IsPartner);
