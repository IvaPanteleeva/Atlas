namespace Atlas.Application.Validation;

using Atlas.Application.Services;
using FluentValidation;

public sealed class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
        => this.RuleForServiceFields(r => r.Name, r => r.Unit, r => r.DefaultRate);
}
