namespace Atlas.Application.Validation;

using Atlas.Application.People;
using FluentValidation;

// Checked on the normalized values (trimmed, empty → null), because that is what PersonRepository stores.
// Names may repeat (two people called Иван), so there is no uniqueness check.
public sealed class SavePersonRequestValidator : AbstractValidator<SavePersonRequest>
{
    public SavePersonRequestValidator()
    {
        this.RuleFor(r => r.Name == null ? null : PersonRules.NormalizeName(r.Name))
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(PersonRules.NameMaxLength)
            .OverridePropertyName("Name")
            .WithName("Име");

        this.RuleFor(r => PersonRules.NormalizeOptional(r.Phone))
            .MaximumLength(PersonRules.PhoneMaxLength)
            .OverridePropertyName("Phone")
            .WithName("Телефон");

        this.RuleFor(r => PersonRules.NormalizeOptional(r.Note))
            .MaximumLength(PersonRules.NoteMaxLength)
            .OverridePropertyName("Note")
            .WithName("Бележка");

        this.RuleFor(r => r.DailyRate)
            .Cascade(CascadeMode.Stop)
            .Money()
            .WithName("Дневна ставка");
    }
}
