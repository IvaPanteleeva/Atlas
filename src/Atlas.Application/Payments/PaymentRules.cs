namespace Atlas.Application.Payments;

// Client payment rules in one place (CLAUDE.md "Плащания и суми"): the validator checks them,
// ClientPaymentConfiguration (Infrastructure) uses the same length and precision.
public static class PaymentRules
{
    public const int NoteMaxLength = 200;

    // numeric(12,2): up to 9 999 999 999.99, room for 1 000 000 (prices are numeric(10,2), up to 100 000).
    public const int AmountPrecision = 12;

    public const decimal MaxAmount = 1_000_000m;

    public const string NotFoundMessage = "Няма такова плащане.";

    public const string MustBePositiveMessage = "Сумата трябва да е по-голяма от 0";

    public const string TooLargeMessage = "Сумата може да е най-много 1 000 000 €";

    public const string TooManyDecimalsMessage = "Сумата може да има най-много 2 знака след запетаята";

    // Trimmed; empty or only spaces means "no note" and is stored as null.
    public static string? NormalizeNote(string? note)
        => string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
