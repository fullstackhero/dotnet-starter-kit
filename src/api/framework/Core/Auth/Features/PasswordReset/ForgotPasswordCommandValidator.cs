using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TcKimlikNo)
            .NotEmpty().WithMessage("TC Kimlik numarası gereklidir")
            .Must(Tckn.IsValid).WithMessage("Geçerli bir TC kimlik numarası giriniz");
    }
}

public class ValidateTcPhoneCommandValidator : AbstractValidator<ValidateTcPhoneCommand>
{
    public ValidateTcPhoneCommandValidator()
    {
        RuleFor(x => x.TcKimlikNo)
            .NotEmpty().WithMessage("TC Kimlik numarası gereklidir")
            .Must(Tckn.IsValid).WithMessage("Geçerli bir TC kimlik numarası giriniz");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir")
            .Must(PhoneNumber.IsValid).WithMessage("Geçerli bir Türkiye telefon numarası giriniz");
    }
} 