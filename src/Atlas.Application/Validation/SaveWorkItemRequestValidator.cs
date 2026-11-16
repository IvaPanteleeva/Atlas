namespace Atlas.Application.Validation;

using Atlas.Application.WorkDays;
using FluentValidation;

// One item of SaveWorkDayRequest; the errors come out as Items[i].Quantity (items[i].quantity in the API).
public sealed class SaveWorkItemRequestValidator : AbstractValidator<SaveWorkItemRequest>
{
    public SaveWorkItemRequestValidator()
    {
        this.RuleFor(i => i.Description == null ? null : i.Description.Trim())
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(WorkDayRules.DescriptionMaxLength)
            .OverridePropertyName("Description")
            .WithName("Описание");

        this.RuleFor(i => i.Quantity)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0m)
            .WithMessage(WorkDayRules.QuantityMustBePositiveMessage)
            .LessThanOrEqualTo(WorkDayRules.MaxQuantity)
            .WithMessage(WorkDayRules.QuantityTooLargeMessage)
            .Must(WorkDayRules.HasAtMostThreeDecimals)
            .WithMessage(WorkDayRules.QuantityTooManyDecimalsMessage);

        // Unit is nullable in the request only so that a missing unit is a 400 here, not LinearMeter.
        this.RuleFor(i => i.Unit)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .IsInEnum()
            .WithName("Мярка");

        // null is allowed: „цена?“.
        this.RuleFor(i => i.Rate)
            .Cascade(CascadeMode.Stop)
            .Money()
            .WithName("Цена");
    }
}
