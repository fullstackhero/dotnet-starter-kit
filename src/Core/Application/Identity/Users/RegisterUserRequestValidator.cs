using DN.WebApi.Application.Common.Validation;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Identity.Users;

public class RegisterUserRequestValidator : CustomValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator(IUserService userService, IStringLocalizer<RegisterUserRequestValidator> localizer)
    {
        RuleFor(u => u.Email).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
                .WithMessage(localizer["Invalid Email Address."])
            .MustAsync(async (email, _) => !await userService.ExistsWithEmailAsync(email))
                .WithMessage((_, email) => string.Format(localizer["Email {0} is already registered."], email));

        RuleFor(u => u.UserName).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(6)
            .MustAsync(async (name, _) => !await userService.ExistsWithNameAsync(name))
                .WithMessage((_, name) => string.Format(localizer["Username {0} is already taken."], name));

        RuleFor(u => u.PhoneNumber).Cascade(CascadeMode.Stop)
            .MustAsync(async (phone, _) => !await userService.ExistsWithPhoneNumberAsync(phone!))
                .WithMessage((_, phone) => string.Format(localizer["Phone number {0} is already registered."], phone))
                .Unless(u => string.IsNullOrWhiteSpace(u.PhoneNumber));

        RuleFor(p => p.FirstName).Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.LastName).Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.Password).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(p => p.ConfirmPassword).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches(p => p.Password);
    }
}