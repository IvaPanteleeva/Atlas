namespace Atlas.Application.WorkDays;

// Work day rules in one place (CLAUDE.md "Работни дни"): the validators check them, the configurations
// (Infrastructure) use the same lengths and precision, WorkDayRepository computes prices and sums with them.
public static class WorkDayRules
{
    public const int TitleMaxLength = 200;

    public const int NoteMaxLength = 2000;

    public const int DescriptionMaxLength = 200;

    public const int MaxItems = 50;

    public const int QuantityPrecision = 10;

    public const int QuantityScale = 3;

    public const decimal MaxQuantity = 100_000m;

    public const string BulgarianTimeZoneId = "Europe/Sofia";

    public const string FutureDateMessage = "Датата не може да е в бъдещето";

    public const string NoItemsMessage = "Добави поне една позиция";

    public const string TooManyItemsMessage = "Най-много 50 позиции в ден";

    public const string QuantityMustBePositiveMessage = "Количеството трябва да е по-голямо от 0";

    public const string QuantityTooLargeMessage = "Количеството може да е най-много 100 000";

    public const string QuantityTooManyDecimalsMessage = "Количеството може да има най-много 3 знака след запетаята";

    private static readonly TimeZoneInfo BulgarianTimeZone = TimeZoneInfo.FindSystemTimeZoneById(BulgarianTimeZoneId);

    // "Today" is the date in Bulgaria, where the work is entered: at 00:30 on the 17th Sofia time the 17th is allowed,
    // although UTC still says the 16th. The clock is injected so the tests can fix the moment.
    public static DateOnly Today(TimeProvider timeProvider)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), BulgarianTimeZone).DateTime);

    // 1.333 is fine, 1.3335 is not (it would be rounded by the numeric(10,3) column).
    public static bool HasAtMostThreeDecimals(decimal quantity)
        => decimal.Round(quantity, QuantityScale) == quantity;

    // Quantity × Rate in cents, half away from zero (0.125 → 0.13, as people count, not banker's rounding).
    // No price („цена?“) means no amount.
    public static decimal? Amount(decimal quantity, decimal? rate)
        => rate is { } value ? Math.Round(quantity * value, 2, MidpointRounding.AwayFromZero) : null;

    // The day's total: only the items with a price (CLAUDE.md "Правила за домейна").
    public static decimal Total(IEnumerable<decimal?> amounts)
        => amounts.Sum(amount => amount ?? 0m);

    // „За уточняване“: no price yet, or flagged. A flagged item with a price still counts in the total.
    public static bool IsOpen(decimal? rate, bool needsReview)
        => rate is null || needsReview;

    public const string WorkerNotFoundMessage = "Няма такъв човек.";

    public const string WorkerNotCrewMessage = "Човекът не е от екипа.";

    public const string WorkerTwiceMessage = "Човекът е избран два пъти.";

    // Who may be put on a day (CLAUDE.md "Хора и профили"): isCrew is null for an unknown person.
    // A new worker must be crew; someone already on the day stays even when no longer crew (old days keep their people).
    public static string? WorkerError(bool isOnTheDay, bool? isCrew)
        => isCrew switch
        {
            null => WorkerNotFoundMessage,
            false when !isOnTheDay => WorkerNotCrewMessage,
            _ => null,
        };

    // Optional text is trimmed; empty or only spaces means "no value" and is stored as null.
    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
