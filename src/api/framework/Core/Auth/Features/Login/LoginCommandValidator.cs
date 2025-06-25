using FluentValidation;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.TcknOrMemberNumber)
            .NotEmpty().WithMessage("TC Kimlik No veya Üye No gereklidir")
            .Must(IsValidTcknOrMemberNumber).WithMessage("Geçerli bir TC kimlik numarası (11 haneli) veya üye numarası giriniz");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir");
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