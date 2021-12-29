using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Shared.DTOs.Identity;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Validators;

public class RegisterUserRequestValidator : CustomValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop).NotEmpty().EmailAddress().WithMessage("Invalid Email Address.");
        RuleFor(p => p.Password).Cascade(CascadeMode.Stop).NotEmpty().MinimumLength(6);
        RuleFor(p => p.UserName).Cascade(CascadeMode.Stop).NotEmpty().MinimumLength(6);
        RuleFor(p => p.FirstName).Cascade(CascadeMode.Stop).NotEmpty();
        RuleFor(p => p.LastName).Cascade(CascadeMode.Stop).NotEmpty();
        RuleFor(p => p.ConfirmPassword).Cascade(CascadeMode.Stop).NotEmpty().Must((model, field) => field == model.Password);
    }
}
