namespace Atlas.Application.Validation;

using Atlas.Application.Common;
using Atlas.Application.Payments;
using FluentValidation;

// The money rules of the API. A null value passes; NotNull decides if it is required.
// Use after Cascade(CascadeMode.Stop) so a bad amount gets one message, not three.
internal static class MoneyRule
{
    // Every price (service default price, site price, work item): more than 0, up to 100 000 €, at most 2 decimals.
    public static IRuleBuilderOptions<T, decimal?> Money<T>(this IRuleBuilder<T, decimal?> rule)
        => rule.Money(
            MoneyRules.MaxAmount,
            MoneyRules.MustBePositiveMessage,
            MoneyRules.TooLargeMessage,
            MoneyRules.TooManyDecimalsMessage);

    // A client payment: the same rule, up to 1 000 000 €, with messages about „Сумата“ (PaymentRules).
    public static IRuleBuilderOptions<T, decimal?> PaymentAmount<T>(this IRuleBuilder<T, decimal?> rule)
        => rule.Money(
            PaymentRules.MaxAmount,
            PaymentRules.MustBePositiveMessage,
            PaymentRules.TooLargeMessage,
            PaymentRules.TooManyDecimalsMessage);

    private static IRuleBuilderOptions<T, decimal?> Money<T>(
        this IRuleBuilder<T, decimal?> rule,
        decimal max,
        string mustBePositiveMessage,
        string tooLargeMessage,
        string tooManyDecimalsMessage)
        => rule
            .GreaterThan(0m)
            .WithMessage(mustBePositiveMessage)
            .LessThanOrEqualTo(max)
            .WithMessage(tooLargeMessage)
            .Must(amount => amount is not { } value || MoneyRules.HasAtMostTwoDecimals(value))
            .WithMessage(tooManyDecimalsMessage);
}
