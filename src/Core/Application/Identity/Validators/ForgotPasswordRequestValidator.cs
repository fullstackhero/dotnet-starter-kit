using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Shared.DTOs.Identity;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Validators;

public class ForgotPasswordRequestValidator : CustomValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop).NotEmpty().EmailAddress().WithMessage("Invalid Email Address.");
    }
}
