namespace Atlas.Application.Validation;

using Atlas.Application.Companies;
using FluentValidation;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
        => this.RuleForCompanyName(r => r.Name);
}
