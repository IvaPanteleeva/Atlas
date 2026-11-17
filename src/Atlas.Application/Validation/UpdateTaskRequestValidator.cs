namespace Atlas.Application.Validation;

using Atlas.Application.Tasks;
using FluentValidation;

// Whether the person exists (and a new one is crew) is SiteTaskRepository's job (400 on assigneeId).
public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
        => this.RuleForText(r => r.Text);
}
