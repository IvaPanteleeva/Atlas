namespace Atlas.Application.Validation;

using Atlas.Application.Common;
using Atlas.Application.Services;
using FluentValidation;

// The field rules shared by CreateServiceRequestValidator and UpdateServiceRequestValidator.
// The name is checked trimmed, because that is what ServiceRepository stores.
// Whether the name + unit is taken is ServiceRepository's job (409), it needs the database.
internal static class ServiceFieldRules
{
    public static void RuleForServiceFields<T>(
        this AbstractValidator<T> validator,
        Func<T, string?> name,
        Func<T, Unit?> unit,
        Func<T, decimal?> defaultRate)
    {
        validator.RuleFor(r => name(r) == null ? null : ServiceRules.NormalizeName(name(r)!))
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(ServiceRules.NameMaxLength)
            .OverridePropertyName("Name")
            .WithName("Име");

        // Unit is nullable in the requests only so that a missing unit is a 400 here, not LinearMeter.
        validator.RuleFor(r => unit(r))
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .IsInEnum()
            .OverridePropertyName("Unit")
            .WithName("Мярка");

        validator.RuleFor(r => defaultRate(r))
            .Cascade(CascadeMode.Stop)
            .Money()
            .OverridePropertyName("DefaultRate")
            .WithName("Цена по подразбиране");
    }
}
