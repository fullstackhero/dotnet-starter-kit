using DN.WebApi.Application.Common.Validation;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Users.Password;

public class ForgotPasswordRequestValidator : CustomValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop).NotEmpty().EmailAddress().WithMessage("Invalid Email Address.");
    }
}
