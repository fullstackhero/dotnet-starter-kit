using FluentValidation;

namespace FSH.Framework.Core.Auth.Features.VerifyRegistration;

public class VerifyRegistrationCommandValidator : AbstractValidator<VerifyRegistrationCommand>
{
    public VerifyRegistrationCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir")
            .Matches(@"^5\d{2}(\s?\d{3}\s?\d{2}\s?\d{2}|\d{7})$")
            .WithMessage("Telefon numarası 5XXXXXXXXX veya 5XX XXX XX XX formatında olmalıdır");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("Doğrulama kodu gereklidir")
            .Length(4).WithMessage("Doğrulama kodu 4 haneli olmalıdır")
            .Matches(@"^\d{4}$").WithMessage("Doğrulama kodu sadece rakamlardan oluşmalıdır");
    }
} 