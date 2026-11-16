namespace Atlas.Application.Validation;

using Atlas.Application.Identity;
using Atlas.Application.Users;
using FluentValidation;

// Shape of the request only. A taken name and an existing company are checked by UserRepository,
// which sees the database. The user name characters are checked here too (same set as Identity),
// so the message is in Bulgarian.
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public const int UserNameMaxLength = 50;

    // Same as IdentityOptionsSetup (Password.RequiredLength).
    public const int PasswordMinLength = 8;

    public CreateUserRequestValidator()
    {
        this.RuleFor(r => r.UserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(UserNameMaxLength)
            .Must(UserNameRules.IsValid)
            .WithMessage(UserNameRules.InvalidCharactersMessage)
            .WithName("Потребителско име");

        this.RuleFor(r => r.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(PasswordMinLength)
            .WithName("Парола");

        this.RuleFor(r => r.Role)
            .Must(role => Roles.All.Contains(role))
            .WithMessage($"Ролята трябва да е една от: {string.Join(", ", Roles.All)}.");

        // Only a ClientViewer belongs to a company; Owner and Manager are our own people.
        this.RuleFor(r => r.CompanyId)
            .NotNull()
            .When(r => r.Role == Roles.ClientViewer)
            .WithMessage("Фирмата е задължителна за ClientViewer.");

        this.RuleFor(r => r.CompanyId)
            .Null()
            .When(r => r.Role is Roles.Owner or Roles.Manager)
            .WithMessage("Само ClientViewer може да има фирма.");
    }
}
