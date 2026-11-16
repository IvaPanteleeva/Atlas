namespace Atlas.Application.Sites;

// Site field rules in one place: the validators check them, SiteConfiguration (Infrastructure)
// uses the same lengths for the columns, and SiteRepository stores the normalized values.
public static class SiteRules
{
    public const int NameMaxLength = 200;

    public const int EndCustomerMaxLength = 200;

    public const int AddressMaxLength = 300;

    public const int MapUrlMaxLength = 500;

    public const int NoteMaxLength = 2000;

    // The name is stored without leading and trailing spaces.
    public static string NormalizeName(string name)
        => name.Trim();

    // Optional text is trimmed; empty or only spaces means "no value" and is stored as null.
    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // The map link opens in a new tab, so only absolute http/https links are allowed
    // (no "maps.google.com" without a scheme, no javascript:, file:, ftp:).
    public static bool IsHttpUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);
}
