namespace Atlas.Application.Validation;

using Atlas.Application.Tasks;
using FluentValidation;

// A missing "done" is an error, not "false": a request without it must not take the tick away by accident.
public sealed class SetTaskDoneRequestValidator : AbstractValidator<SetTaskDoneRequest>
{
    public SetTaskDoneRequestValidator()
        => this.RuleFor(r => r.Done)
            .NotNull()
            .WithName("Отметка");
}
