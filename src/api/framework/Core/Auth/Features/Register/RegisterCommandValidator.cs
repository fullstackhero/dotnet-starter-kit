using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gereklidir")
            .Must(Email.IsValid).WithMessage("Geçerli bir email adresi giriniz");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı gereklidir")
            .Length(3, 20).WithMessage("Kullanıcı adı 3-20 karakter arasında olmalıdır")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Kullanıcı adı sadece harf, rakam ve alt çizgi içerebilir");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir")
            .Must(PhoneNumber.IsValid).WithMessage("Geçerli bir Türkiye telefon numarası giriniz");

        RuleFor(x => x.Tckn)
            .NotEmpty().WithMessage("TC Kimlik numarası gereklidir")
            .Must(Tckn.IsValid).WithMessage("Geçerli bir TC kimlik numarası giriniz");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir")
            .Must(Password.IsValid).WithMessage("Şifre güçlü olmalıdır (en az 8 karakter, büyük/küçük harf, rakam ve özel karakter)");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir")
            .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir")
            .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir");

        RuleFor(x => x.Profession)
            .MaximumLength(100).WithMessage("Meslek en fazla 100 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Profession));
    }
} 