namespace Atlas.Application.Validation;

using Atlas.Application.Common;
using FluentValidation;

// The money rule shared by every price in the API (service default price, site price, later work items and payments):
// more than 0, up to 100 000 €, at most 2 decimals (MoneyRules). A null value passes; NotNull decides if it is required.
// Use after Cascade(CascadeMode.Stop) so a bad price gets one message, not three.
internal static class MoneyRule
{
    public static IRuleBuilderOptions<T, decimal?> Money<T>(this IRuleBuilder<T, decimal?> rule)
        => rule
            .GreaterThan(0m)
            .WithMessage(MoneyRules.MustBePositiveMessage)
            .LessThanOrEqualTo(MoneyRules.MaxAmount)
            .WithMessage(MoneyRules.TooLargeMessage)
            .Must(amount => amount is not { } value || MoneyRules.HasAtMostTwoDecimals(value))
            .WithMessage(MoneyRules.TooManyDecimalsMessage);
}
