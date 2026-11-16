namespace Atlas.Application.Validation;

using Atlas.Application.Sites;
using FluentValidation;

// The field rules shared by CreateSiteRequestValidator and UpdateSiteRequestValidator.
// Checked on the normalized values (trimmed, empty → null), because that is what SiteRepository stores
// (FluentValidation 12 has no Transform, so each rule is on an expression with the property name set back).
// Whether the company exists is SiteRepository's job (400 on companyId), it needs the database.
internal static class SiteFieldRules
{
    public const string MapUrlMessage = "Линкът трябва да започва с http:// или https://";

    public static void RuleForSiteFields<T>(
        this AbstractValidator<T> validator,
        Func<T, string?> name,
        Func<T, string?> endCustomer,
        Func<T, string?> address,
        Func<T, string?> mapUrl,
        Func<T, string?> note)
    {
        validator.RuleFor(r => name(r) == null ? null : SiteRules.NormalizeName(name(r)!))
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(SiteRules.NameMaxLength)
            .OverridePropertyName("Name")
            .WithName("Име");

        validator.RuleForOptional(endCustomer, SiteRules.EndCustomerMaxLength, "EndCustomer", "Краен клиент");
        validator.RuleForOptional(address, SiteRules.AddressMaxLength, "Address", "Адрес");
        validator.RuleForOptional(note, SiteRules.NoteMaxLength, "Note", "Бележка");

        validator.RuleFor(r => SiteRules.NormalizeOptional(mapUrl(r)))
            .Cascade(CascadeMode.Stop)
            .MaximumLength(SiteRules.MapUrlMaxLength)
            .Must(url => url is null || SiteRules.IsHttpUrl(url))
            .WithMessage(MapUrlMessage)
            .OverridePropertyName("MapUrl")
            .WithName("Линк към карта");
    }

    private static void RuleForOptional<T>(
        this AbstractValidator<T> validator,
        Func<T, string?> value,
        int maxLength,
        string propertyName,
        string displayName)
        => validator.RuleFor(r => SiteRules.NormalizeOptional(value(r)))
            .MaximumLength(maxLength)
            .OverridePropertyName(propertyName)
            .WithName(displayName);
}
