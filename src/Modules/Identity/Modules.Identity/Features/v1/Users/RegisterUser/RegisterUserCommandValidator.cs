using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;

namespace FSH.Modules.Identity.Features.v1.Users.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("First name"))
            .MaximumLength(100).WithMessage(IdentityValidationMessages.MaxLength("First name", 100));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Last name"))
            .MaximumLength(100).WithMessage(IdentityValidationMessages.MaxLength("Last name", 100));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Email"))
            .EmailAddress().WithMessage(IdentityValidationMessages.InvalidEmail());

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Username"))
            .MinimumLength(3).WithMessage(IdentityValidationMessages.MinLength("Username", 3))
            .MaximumLength(50).WithMessage(IdentityValidationMessages.MaxLength("Username", 50));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Password"))
            .MinimumLength(6).WithMessage(IdentityValidationMessages.MinLength("Password", 6));

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Password confirmation"))
            .Equal(x => x.Password).WithMessage(IdentityValidationMessages.PasswordsMustMatch());

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage(IdentityValidationMessages.MaxLength("Phone number", 20))
            .When(x => x.PhoneNumber is not null);
    }
}