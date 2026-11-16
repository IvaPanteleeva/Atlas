namespace Atlas.Application.Validation;

using Atlas.Application.Auth;
using FluentValidation;

// Only "both filled in"; whether they match is the sign-in's job (401, not 400).
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        this.RuleFor(r => r.UserName)
            .NotEmpty()
            .WithName("Потребителско име");

        this.RuleFor(r => r.Password)
            .NotEmpty()
            .WithName("Парола");
    }
}
