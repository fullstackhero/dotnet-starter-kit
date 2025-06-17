using FluentValidation;

namespace FSH.Framework.Core.Auth.Features.RegisterRequest;

public class RegisterRequestCommandValidator : AbstractValidator<RegisterRequestCommand>
{
    public RegisterRequestCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gereklidir")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı gereklidir")
            .Length(3, 50).WithMessage("Kullanıcı adı 3-50 karakter arasında olmalıdır");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir")
            .Matches(@"^5\d{2}(\s?\d{3}\s?\d{2}\s?\d{2}|\d{7})$")
            .WithMessage("Telefon numarası 5XXXXXXXXX veya 5XX XXX XX XX formatında olmalıdır");

        RuleFor(x => x.Tckn)
            .NotEmpty().WithMessage("TC Kimlik No gereklidir")
            .Length(11).WithMessage("TC Kimlik No 11 haneli olmalıdır")
            .Matches(@"^\d{11}$").WithMessage("TC Kimlik No sadece rakamlardan oluşmalıdır");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-\.#])[A-Za-z\d@$!%*?&_\-\.#]{8,}$")
            .WithMessage("Şifre en az 1 büyük harf, 1 küçük harf, 1 rakam ve 1 özel karakter içermelidir");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir")
            .Length(2, 50).WithMessage("Ad 2-50 karakter arasında olmalıdır");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir")
            .Length(2, 50).WithMessage("Soyad 2-50 karakter arasında olmalıdır");

        RuleFor(x => x.ProfessionId)
            .GreaterThan(0).WithMessage("Meslek alanı seçilmelidir");

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Today).WithMessage("Doğum tarihi geçerli olmalıdır")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.RegistrationIp)
            .NotEmpty().WithMessage("Kayıt IP adresi gereklidir");

        RuleFor(x => x.DeviceInfo)
            .NotEmpty().WithMessage("Cihaz bilgisi gereklidir");
    }
} 