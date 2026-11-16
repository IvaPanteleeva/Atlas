namespace Atlas.Application.Validation;

using Atlas.Application.Companies;
using FluentValidation;

// The Name rule shared by CreateCompanyRequestValidator and UpdateCompanyRequestValidator.
// Checked on the trimmed value, because that is what CompanyRepository stores
// (FluentValidation 12 has no Transform, so the rule is on an expression with the property name set back).
// Whether the name is taken is CompanyRepository's job (409), it needs the database.
internal static class CompanyNameRule
{
    public static void RuleForCompanyName<T>(this AbstractValidator<T> validator, Func<T, string?> name)
        => validator.RuleFor(r => name(r) == null ? null : CompanyRules.NormalizeName(name(r)!))
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(CompanyRules.NameMaxLength)
            .OverridePropertyName("Name")
            .WithName("Име");
}
