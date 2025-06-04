using MediatR;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using PhoneNumberVO = FSH.Framework.Core.Auth.Domain.ValueObjects.PhoneNumber;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ResetPasswordCommand : IRequest<string>
{
    public string TcKimlikNo { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string SmsCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    
    // Domain validation methods
    public bool IsValid() => 
        Tckn.IsValid(TcKimlikNo) && 
        PhoneNumberVO.IsValid(PhoneNumber) &&
        Password.IsValid(NewPassword) &&
        IsValidSmsCode();
    
    private bool IsValidSmsCode() =>
        !string.IsNullOrWhiteSpace(SmsCode) && 
        SmsCode.Length == 6 && 
        SmsCode.All(char.IsDigit);
    
    // Get domain value objects
    public Tckn GetTcKimlik() => Tckn.CreateUnsafe(TcKimlikNo);
    public PhoneNumberVO GetPhoneNumber() => PhoneNumberVO.CreateUnsafe(PhoneNumber);
    public Password GetPassword() => Password.CreateUnsafe(NewPassword);
} 