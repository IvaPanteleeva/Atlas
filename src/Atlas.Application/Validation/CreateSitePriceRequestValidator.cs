namespace Atlas.Application.Validation;

using Atlas.Application.SitePrices;
using FluentValidation;

// Whether the service exists is SitePriceRepository's job (400 on serviceId), it needs the database.
public sealed class CreateSitePriceRequestValidator : AbstractValidator<CreateSitePriceRequest>
{
    public CreateSitePriceRequestValidator()
    {
        this.RuleFor(r => r.ServiceId)
            .NotEmpty()
            .WithName("Услуга");

        this.RuleForSitePriceRate(r => r.Rate);
    }
}
