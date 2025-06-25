using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TcknOrMemberNumber)
            .NotEmpty().WithMessage("TC Kimlik numarası veya Üye numarası gereklidir")
            .Must(IsValidTcknOrMemberNumber).WithMessage("Geçerli bir TC kimlik numarası (11 haneli) veya üye numarası giriniz");
    }
    
    private static bool IsValidTcknOrMemberNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
            
        // TC Kimlik numarası kontrolü (11 haneli)
        if (input.Length == 11 && input.All(char.IsDigit))
        {
            return Tckn.IsValid(input);
        }
        
        // Üye numarası kontrolü (alfanumerik, 6-20 karakter)
        if (input.Length >= 6 && input.Length <= 20)
        {
            return input.All(c => char.IsLetterOrDigit(c));
        }
        
        return false;
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