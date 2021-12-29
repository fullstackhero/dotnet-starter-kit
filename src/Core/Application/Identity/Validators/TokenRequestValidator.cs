using DN.WebApi.Application.Common.Validation;
using DN.WebApi.Shared.DTOs.Identity;
using FluentValidation;

namespace DN.WebApi.Application.Identity.Validators;

public class TokenRequestValidator : CustomValidator<TokenRequest>
{
    public TokenRequestValidator()
    {
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop).NotEmpty().EmailAddress().WithMessage("Invalid Email Address.");
        RuleFor(p => p.Password).Cascade(CascadeMode.Stop).NotEmpty();
    }
}
