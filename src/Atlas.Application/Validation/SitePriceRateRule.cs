namespace Atlas.Application.Validation;

using FluentValidation;

// The Rate rule shared by CreateSitePriceRequestValidator and UpdateSitePriceRequestValidator:
// a site price always has a value (Rate is nullable in the requests only so a missing one is a 400).
internal static class SitePriceRateRule
{
    public static void RuleForSitePriceRate<T>(this AbstractValidator<T> validator, Func<T, decimal?> rate)
        => validator.RuleFor(r => rate(r))
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Money()
            .OverridePropertyName("Rate")
            .WithName("Цена");
}
