using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Shared.DTOs.Identity;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Validators;

public class ChangePasswordRequestValidator : CustomValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(p => p.Password).NotEmpty();
        RuleFor(p => p.NewPassword).NotEmpty();
        RuleFor(p => p.ConfirmNewPassword).Equal(p => p.NewPassword).WithMessage("Passwords do not match.");
    }
}