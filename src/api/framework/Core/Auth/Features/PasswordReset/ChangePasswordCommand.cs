using MediatR;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ChangePasswordCommand : IRequest<string>
{
    [JsonPropertyName("tcKimlikNo")]
    public string TcKimlikNo { get; set; } = string.Empty;
    
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
    
    [JsonPropertyName("confirmNewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
    
    // Domain validation method
    public bool IsValid() => 
        Tckn.IsValid(TcKimlikNo) && 
        !string.IsNullOrWhiteSpace(CurrentPassword) &&
        Password.IsValid(NewPassword) &&
        NewPassword == ConfirmNewPassword;
    
    // Get domain value objects
    public Tckn GetTcKimlik() => Tckn.CreateUnsafe(TcKimlikNo);
    public Password GetCurrentPassword() => Password.CreateUnsafe(CurrentPassword);
    public Password GetPassword() => Password.CreateUnsafe(NewPassword);
} 