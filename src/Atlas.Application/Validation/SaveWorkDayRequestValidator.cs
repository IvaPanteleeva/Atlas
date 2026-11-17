namespace Atlas.Application.Validation;

using Atlas.Application.WorkDays;
using FluentValidation;

// Checked on the normalized values (trimmed, empty → null), because that is what WorkDayRepository stores.
// Whether the site and the services exist is WorkDayRepository's job (404 / 400 on items[i].serviceId).
// The clock is injected (TimeProvider.System in the app) so "today" can be fixed in the tests.
public sealed class SaveWorkDayRequestValidator : AbstractValidator<SaveWorkDayRequest>
{
    public SaveWorkDayRequestValidator(TimeProvider timeProvider)
    {
        this.RuleFor(r => r.Date)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(date => date <= WorkDayRules.Today(timeProvider))
            .WithMessage(WorkDayRules.FutureDateMessage)
            .WithName("Дата");

        this.RuleFor(r => WorkDayRules.NormalizeOptional(r.Title))
            .MaximumLength(WorkDayRules.TitleMaxLength)
            .OverridePropertyName("Title")
            .WithName("Заглавие");

        this.RuleFor(r => WorkDayRules.NormalizeOptional(r.Note))
            .MaximumLength(WorkDayRules.NoteMaxLength)
            .OverridePropertyName("Note")
            .WithName("Бележка");

        this.RuleFor(r => r.Items)
            .Cascade(CascadeMode.Stop)
            .Must(items => items is { Count: > 0 })
            .WithMessage(WorkDayRules.NoItemsMessage)
            .Must(items => items!.Count <= WorkDayRules.MaxItems)
            .WithMessage(WorkDayRules.TooManyItemsMessage);

        this.RuleForEach(r => r.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithName("Позиция")
            .SetValidator(new SaveWorkItemRequestValidator());

        // Each person once. Whether they exist and are crew is WorkDayRepository's job (400 on workers).
        this.RuleFor(r => r.Workers)
            .Must(workers => workers is null
                || workers.Where(w => w is not null).Select(w => w.PersonId).Distinct().Count()
                    == workers.Count(w => w is not null))
            .WithMessage(WorkDayRules.WorkerTwiceMessage);

        // The pay for the day: the price rule (more than 0, up to 100 000, 2 decimals) or empty („за уточняване“).
        this.RuleForEach(r => r.Workers)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithName("Човек")
            .ChildRules(worker => worker
                .RuleFor(w => w.Pay)
                .Cascade(CascadeMode.Stop)
                .Money()
                .WithName("Сума за деня"));
    }
}
