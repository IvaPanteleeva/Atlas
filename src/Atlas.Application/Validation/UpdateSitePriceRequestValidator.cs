namespace Atlas.Application.Validation;

using Atlas.Application.SitePrices;
using FluentValidation;

public sealed class UpdateSitePriceRequestValidator : AbstractValidator<UpdateSitePriceRequest>
{
    public UpdateSitePriceRequestValidator()
        => this.RuleForSitePriceRate(r => r.Rate);
}
