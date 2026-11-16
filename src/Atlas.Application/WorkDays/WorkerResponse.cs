namespace Atlas.Application.WorkDays;

/// <summary>A person who worked on a work day.</summary>
/// <param name="Id">Person id.</param>
/// <param name="Name">The person's current name.</param>
public sealed record WorkerResponse(Guid Id, string Name);
