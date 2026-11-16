namespace Atlas.Application.Validation;

using Atlas.Application.Sites;
using FluentValidation;

// Status is required on update: PUT replaces the whole site, a missing status must not become Lead.
public sealed class UpdateSiteRequestValidator : AbstractValidator<UpdateSiteRequest>
{
    public UpdateSiteRequestValidator()
    {
        this.RuleForSiteFields(r => r.Name, r => r.EndCustomer, r => r.Address, r => r.MapUrl, r => r.Note);

        this.RuleFor(r => r.Status)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .IsInEnum()
            .WithName("Статус");
    }
}
