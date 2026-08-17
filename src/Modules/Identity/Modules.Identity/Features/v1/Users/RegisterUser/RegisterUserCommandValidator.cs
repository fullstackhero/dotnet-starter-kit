using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Users.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(_ => localizer["Validation.FirstNameRequired"])
            .MaximumLength(100).WithMessage(_ => localizer["Validation.FirstNameMaxLength"]);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(_ => localizer["Validation.LastNameRequired"])
            .MaximumLength(100).WithMessage(_ => localizer["Validation.LastNameMaxLength"]);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => localizer["Validation.EmailRequired"])
            .EmailAddress().WithMessage(_ => localizer["Validation.EmailInvalid"]);

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage(_ => localizer["Validation.UsernameRequired"])
            .MinimumLength(3).WithMessage(_ => localizer["Validation.UsernameMinLength"])
            .MaximumLength(50).WithMessage(_ => localizer["Validation.UsernameMaxLength"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => localizer["Validation.PasswordRequired"])
            .MinimumLength(6).WithMessage(_ => localizer["Validation.PasswordMinLength"]);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(_ => localizer["Validation.PasswordConfirmationRequired"])
            .Equal(x => x.Password).WithMessage(_ => localizer["Validation.PasswordsDoNotMatch"]);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage(_ => localizer["Validation.PhoneNumberMaxLength"])
            .When(x => x.PhoneNumber is not null);
    }
}