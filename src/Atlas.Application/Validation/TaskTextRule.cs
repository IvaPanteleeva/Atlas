namespace Atlas.Application.Validation;

using Atlas.Application.Tasks;
using FluentValidation;

// The task text, the same for adding and editing: checked trimmed, because that is what SiteTaskRepository stores.
internal static class TaskTextRule
{
    public static void RuleForText<T>(this AbstractValidator<T> validator, Func<T, string?> text)
        => validator.RuleFor(r => text(r) == null ? null : TaskRules.NormalizeText(text(r)!))
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(TaskRules.TextMaxLength)
            .OverridePropertyName("Text")
            .WithName("Текст");
}
