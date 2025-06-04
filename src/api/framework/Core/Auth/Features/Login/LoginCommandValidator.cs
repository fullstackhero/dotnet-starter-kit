using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Tckn)
            .NotEmpty().WithMessage("TC Kimlik No gereklidir")
            .Must(tckn => Tckn.IsValid(tckn.Value)).WithMessage("Geçerli bir TC kimlik numarası giriniz");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir");
    }
} 