namespace Atlas.Application.Validation;

using Atlas.Application.Tasks;
using FluentValidation;

// Whether the site and the person exist (and the person is crew) is SiteTaskRepository's job (400 on the field).
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
        => this.RuleForText(r => r.Text);
}
