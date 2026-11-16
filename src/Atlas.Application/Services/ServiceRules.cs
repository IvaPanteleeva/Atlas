namespace Atlas.Application.Services;

// Service field rules in one place: the validators check them, ServiceConfiguration (Infrastructure)
// uses the same length for the column, and ServiceRepository stores the normalized name.
public static class ServiceRules
{
    public const int NameMaxLength = 200;

    // The name is stored and compared without leading and trailing spaces.
    public static string NormalizeName(string name)
        => name.Trim();

    // 409 when deleting a service that has prices on sites: „Услугата има цени в 3 обекта“ / „в 1 обект“.
    public static string InUseMessage(int siteCount)
        => $"Услугата има цени в {siteCount} {(siteCount == 1 ? "обект" : "обекта")}";
}
