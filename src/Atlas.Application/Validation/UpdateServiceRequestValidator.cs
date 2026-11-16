namespace Atlas.Application.Validation;

using Atlas.Application.Services;
using FluentValidation;

// PUT replaces the whole service: same rules as when adding.
public sealed class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
        => this.RuleForServiceFields(r => r.Name, r => r.Unit, r => r.DefaultRate);
}
