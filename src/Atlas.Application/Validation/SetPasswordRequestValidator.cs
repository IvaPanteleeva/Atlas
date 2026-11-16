namespace Atlas.Application.Validation;

using Atlas.Application.Users;
using FluentValidation;

// The same password rule as when creating a user (IdentityOptionsSetup: at least 8 characters, nothing else),
// checked here first so the message is in Bulgarian.
public sealed class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
        => this.RuleFor(r => r.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(CreateUserRequestValidator.PasswordMinLength)
            .WithName("Парола");
}
