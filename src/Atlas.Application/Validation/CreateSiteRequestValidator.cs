namespace Atlas.Application.Validation;

using Atlas.Application.Sites;
using FluentValidation;

// Status is optional here: a new site without one is Lead (SiteRepository).
public sealed class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        this.RuleForSiteFields(r => r.Name, r => r.EndCustomer, r => r.Address, r => r.MapUrl, r => r.Note);

        this.RuleFor(r => r.Status)
            .IsInEnum()
            .WithName("Статус");
    }
}
