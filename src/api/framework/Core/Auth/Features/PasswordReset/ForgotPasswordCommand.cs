using MediatR;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using System.Text.Json.Serialization;
using PhoneNumberVO = FSH.Framework.Core.Auth.Domain.ValueObjects.PhoneNumber;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
{
    [JsonPropertyName("tcknOrMemberNumber")]
    public string TcknOrMemberNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("birthDate")]
    public DateTime BirthDate { get; set; }
    
    // Domain validation method
    public bool IsValid() => IsValidTcknOrMemberNumber(TcknOrMemberNumber) && BirthDate != default;
    
    // Get domain value object for TCKN (only if it's a TCKN)
    public Tckn? GetTcKimlik() => 
        TcknOrMemberNumber.Length == 11 && TcknOrMemberNumber.All(char.IsDigit) 
            ? Tckn.CreateUnsafe(TcknOrMemberNumber) 
            : null;
    
    private bool IsValidTcknOrMemberNumber(string input)
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

public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MaskedEmail { get; set; }
    public string? MaskedPhone { get; set; }
    public bool HasEmail { get; set; }
    public bool HasPhone { get; set; }
}

public class SelectResetMethodCommand : IRequest<string>
{
    [JsonPropertyName("tcknOrMemberNumber")]
    public string TcknOrMemberNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("birthDate")]
    public DateTime BirthDate { get; set; }
    
    [JsonPropertyName("method")]
    public ResetMethod Method { get; set; }
    
    public bool IsValid() => IsValidTcknOrMemberNumber(TcknOrMemberNumber) && BirthDate != default && Enum.IsDefined(typeof(ResetMethod), Method);
    
    public Tckn? GetTcKimlik() => 
        TcknOrMemberNumber.Length == 11 && TcknOrMemberNumber.All(char.IsDigit) 
            ? Tckn.CreateUnsafe(TcknOrMemberNumber) 
            : null;
    
    private bool IsValidTcknOrMemberNumber(string input)
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

public enum ResetMethod
{
    Email = 1,
    Sms = 2
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