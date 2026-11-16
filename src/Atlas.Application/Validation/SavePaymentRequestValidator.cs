namespace Atlas.Application.Validation;

using Atlas.Application.Payments;
using Atlas.Application.WorkDays;
using FluentValidation;

// Checked on the normalized note (trimmed, empty → null), because that is what PaymentRepository stores.
// Whether the site and the payment exist is PaymentRepository's job (404).
// The clock is injected (TimeProvider.System in the app) so "today" can be fixed in the tests.
public sealed class SavePaymentRequestValidator : AbstractValidator<SavePaymentRequest>
{
    public SavePaymentRequestValidator(TimeProvider timeProvider)
    {
        // Same "today" as the work days: the date in Bulgaria.
        this.RuleFor(r => r.Date)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(date => date <= WorkDayRules.Today(timeProvider))
            .WithMessage(WorkDayRules.FutureDateMessage)
            .WithName("Дата");

        this.RuleFor(r => r.Amount)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .PaymentAmount()
            .WithName("Сума");

        this.RuleFor(r => PaymentRules.NormalizeNote(r.Note))
            .MaximumLength(PaymentRules.NoteMaxLength)
            .OverridePropertyName("Note")
            .WithName("Бележка");
    }
}
