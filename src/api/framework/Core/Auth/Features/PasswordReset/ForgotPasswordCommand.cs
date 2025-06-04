using MediatR;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using PhoneNumberVO = FSH.Framework.Core.Auth.Domain.ValueObjects.PhoneNumber;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ForgotPasswordCommand : IRequest<string>
{
    public string TcKimlikNo { get; set; } = string.Empty;
    
    // Domain validation method
    public bool IsValid() => Tckn.IsValid(TcKimlikNo);
    
    // Get domain value object
    public Tckn GetTcKimlik() => Tckn.CreateUnsafe(TcKimlikNo);
}

public class ValidateTcPhoneCommand : IRequest<string>
{
    public string TcKimlikNo { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    
    // Domain validation methods
    public bool IsValid() => Tckn.IsValid(TcKimlikNo) && PhoneNumberVO.IsValid(PhoneNumber);
    
    // Get domain value objects
    public Tckn GetTcKimlik() => Tckn.CreateUnsafe(TcKimlikNo);
    public PhoneNumberVO GetPhoneNumber() => PhoneNumberVO.CreateUnsafe(PhoneNumber);
} 