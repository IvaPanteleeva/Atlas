namespace Atlas.Application.Validation;

using Atlas.Application.Identity;
using Atlas.Application.Users;
using FluentValidation;

// Shape of the request only. Allowed user name characters, a taken name and an existing company
// are checked by Identity and UserRepository, which see the database.
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public const int UserNameMaxLength = 50;

    // Same as IdentityOptionsSetup (Password.RequiredLength).
    public const int PasswordMinLength = 8;

    public CreateUserRequestValidator()
    {
        this.RuleFor(r => r.UserName)
            .NotEmpty()
            .MaximumLength(UserNameMaxLength)
            .WithName("Потребителско име");

        this.RuleFor(r => r.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(PasswordMinLength)
            .WithName("Парола");

        this.RuleFor(r => r.Role)
            .Must(role => Roles.All.Contains(role))
            .WithMessage($"Ролята трябва да е една от: {string.Join(", ", Roles.All)}.");

        // Only a ClientViewer belongs to a company; Owner and Crew are our own people.
        this.RuleFor(r => r.CompanyId)
            .NotNull()
            .When(r => r.Role == Roles.ClientViewer)
            .WithMessage("Фирмата е задължителна за ClientViewer.");

        this.RuleFor(r => r.CompanyId)
            .Null()
            .When(r => r.Role is Roles.Owner or Roles.Crew)
            .WithMessage("Само ClientViewer може да има фирма.");
    }
}
