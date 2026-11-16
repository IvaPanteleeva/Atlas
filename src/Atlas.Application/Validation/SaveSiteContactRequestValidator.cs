namespace Atlas.Application.Validation;

using Atlas.Application.People;
using Atlas.Application.SiteContacts;
using FluentValidation;

// Checked on the normalized values (trimmed, empty → null), because that is what SiteContactRepository stores.
// Whether the site and the contact exist is SiteContactRepository's job (404).
public sealed class SaveSiteContactRequestValidator : AbstractValidator<SaveSiteContactRequest>
{
    public SaveSiteContactRequestValidator()
    {
        this.RuleFor(r => r.Name == null ? null : PersonRules.NormalizeName(r.Name))
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(SiteContactRules.NameMaxLength)
            .OverridePropertyName("Name")
            .WithName("Име");

        this.RuleFor(r => PersonRules.NormalizeOptional(r.Role))
            .MaximumLength(SiteContactRules.RoleMaxLength)
            .OverridePropertyName("Role")
            .WithName("Роля");

        this.RuleFor(r => PersonRules.NormalizeOptional(r.Phone))
            .MaximumLength(SiteContactRules.PhoneMaxLength)
            .OverridePropertyName("Phone")
            .WithName("Телефон");

        this.RuleFor(r => PersonRules.NormalizeOptional(r.Note))
            .MaximumLength(SiteContactRules.NoteMaxLength)
            .OverridePropertyName("Note")
            .WithName("Бележка");
    }
}
