using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.TcKimlikNo)
            .NotEmpty().WithMessage("TC Kimlik numarası gereklidir")
            .Must(Tckn.IsValid).WithMessage("Geçerli bir TC kimlik numarası giriniz");

        RuleFor(x => x.CurrentPasswordValue)
            .NotEmpty().WithMessage("Mevcut şifre gereklidir");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre gereklidir")
            .Must(Password.IsValid).WithMessage("Geçersiz şifre. Şifre en az 8 karakter olmalı ve büyük harf, küçük harf, rakam ve özel karakter içermelidir.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Şifre tekrarı gereklidir")
            .Equal(x => x.NewPassword).WithMessage("Lütfen aynı şifreleri giriniz.");
            
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPasswordValue).WithMessage("Yeni şifre mevcut şifreden farklı olmalıdır.");
    }
}