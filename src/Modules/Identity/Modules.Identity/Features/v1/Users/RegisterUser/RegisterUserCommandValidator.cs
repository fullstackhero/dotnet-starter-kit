using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;

namespace FSH.Modules.Identity.Features.v1.Users.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(IdentityValidationMessages.FirstNameRequired)
            .MaximumLength(100).WithMessage(IdentityValidationMessages.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(IdentityValidationMessages.LastNameRequired)
            .MaximumLength(100).WithMessage(IdentityValidationMessages.LastNameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(IdentityValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(IdentityValidationMessages.InvalidEmail);

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage(IdentityValidationMessages.UsernameRequired)
            .MinimumLength(3).WithMessage(IdentityValidationMessages.UsernameMinLength)
            .MaximumLength(50).WithMessage(IdentityValidationMessages.UsernameMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(IdentityValidationMessages.PasswordRequired)
            .MinimumLength(6).WithMessage(IdentityValidationMessages.PasswordMinLength);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(IdentityValidationMessages.PasswordConfirmationRequired)
            .Equal(x => x.Password).WithMessage(IdentityValidationMessages.PasswordsMustMatch);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage(IdentityValidationMessages.PhoneNumberMaxLength)
            .When(x => x.PhoneNumber is not null);
    }
}