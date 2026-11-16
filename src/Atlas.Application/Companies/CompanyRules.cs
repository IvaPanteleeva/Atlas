namespace Atlas.Application.Companies;

// Company name rules in one place: the validators check them, CompanyConfiguration (Infrastructure)
// uses the same length for the column, and CompanyRepository stores the normalized name.
public static class CompanyRules
{
    public const int NameMaxLength = 200;

    // The name is stored and compared without leading and trailing spaces.
    public static string NormalizeName(string name)
        => name.Trim();
}
