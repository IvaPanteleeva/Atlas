namespace Atlas.Application.Portal;

/// <summary>„Дължите“ over the client's sites. An overpaid site does not lower the debt of another one.</summary>
/// <param name="Owed">Sum of the sites' Remaining where it is above 0, in EUR.</param>
/// <param name="Overpaid">Sum of the overpayments (−Remaining where Remaining is below 0), in EUR.</param>
/// <param name="OverpaidSiteCount">Number of sites with Remaining below 0.</param>
public sealed record PortalTotalsResponse(
    decimal Owed,
    decimal Overpaid,
    int OverpaidSiteCount);
