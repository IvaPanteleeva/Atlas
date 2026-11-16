namespace Atlas.Application.Common;

// Money rules in one place (CLAUDE.md "Правила за домейна": money is always decimal).
// The validators check them (Validation/MoneyRule.cs) and the configurations (Infrastructure)
// use the same precision for the columns: numeric(10,2).
public static class MoneyRules
{
    public const int Precision = 10;

    public const int Scale = 2;

    public const decimal MaxAmount = 100_000m;

    public const string MustBePositiveMessage = "Цената трябва да е по-голяма от 0";

    public const string TooLargeMessage = "Цената може да е най-много 100 000 €";

    public const string TooManyDecimalsMessage = "Цената може да има най-много 2 знака след запетаята";

    // 6.5 and 6.55 are fine, 6.555 is not (it would be rounded by the numeric(10,2) column).
    public static bool HasAtMostTwoDecimals(decimal amount)
        => decimal.Round(amount, Scale) == amount;
}
