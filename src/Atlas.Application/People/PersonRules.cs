namespace Atlas.Application.People;

// Person rules in one place (CLAUDE.md "Хора и профили"): the validator checks them, PersonConfiguration
// (Infrastructure) uses the same lengths, PersonRepository stores the normalized values and counts the days.
public static class PersonRules
{
    public const int NameMaxLength = 100;

    public const int PhoneMaxLength = 30;

    public const int NoteMaxLength = 500;

    public const string NotFoundMessage = "Няма такъв човек.";

    public const string HasWorkDaysMessage = "Човекът има работни дни";

    public const string LinkedToUserMessage = "Човекът е свързан с профил за вход.";

    // The name is stored without leading and trailing spaces.
    public static string NormalizeName(string name)
        => name.Trim();

    // Optional text is trimmed; empty or only spaces means "no value" and is stored as null.
    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // „Изкарал“ (CLAUDE.md "Разход за хора"): the person's pay over all their days; days without a pay add nothing.
    public static decimal Earned(IEnumerable<decimal?> pays)
        => pays.Sum(pay => pay ?? 0m);

    // Days per person: distinct dates over all sites (two sites on one date = 1 day, CLAUDE.md "Хора и профили"),
    // and the latest of them.
    public static (int DayCount, DateOnly? LastWorkDate) WorkStats(IEnumerable<DateOnly> workDates)
    {
        var dates = workDates.Distinct().ToList();
        return (dates.Count, dates.Count == 0 ? null : dates.Max());
    }
}
