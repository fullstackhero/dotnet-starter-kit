using DN.WebApi.Application.Common.Validation;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Users.Password;

public class ChangePasswordRequestValidator : CustomValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(p => p.Password).NotEmpty();
        RuleFor(p => p.NewPassword).NotEmpty();
        RuleFor(p => p.ConfirmNewPassword).Equal(p => p.NewPassword).WithMessage("Passwords do not match.");
    }
}