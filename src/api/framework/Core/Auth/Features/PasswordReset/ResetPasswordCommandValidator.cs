using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.TcKimlikNo)
            .NotEmpty().WithMessage("TC Kimlik numarası gereklidir")
            .Must(Tckn.IsValid).WithMessage("Geçerli bir TC kimlik numarası giriniz");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir")
            .Must(PhoneNumber.IsValid).WithMessage("Geçerli bir Türkiye telefon numarası giriniz");

        RuleFor(x => x.SmsCode)
            .NotEmpty().WithMessage("SMS kodu gereklidir")
            .Length(6).WithMessage("SMS kodu 6 haneli olmalıdır")
            .Must(code => code.All(char.IsDigit)).WithMessage("SMS kodu sadece rakam içermelidir");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre gereklidir")
            .Must(Password.IsValid).WithMessage("Şifre güçlü olmalıdır (en az 8 karakter, büyük/küçük harf, rakam ve özel karakter)");
    }
} 