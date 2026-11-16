namespace Atlas.Application.Validation;

using Atlas.Application.Companies;
using FluentValidation;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
        => this.RuleForCompanyName(r => r.Name);
}
