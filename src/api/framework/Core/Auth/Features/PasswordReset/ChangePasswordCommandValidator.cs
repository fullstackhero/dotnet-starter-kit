using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID gereklidir");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre gereklidir");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre gereklidir")
            .Must(Password.IsValid).WithMessage("Şifre güçlü olmalıdır (en az 8 karakter, büyük/küçük harf, rakam ve özel karakter)");
    }
} 