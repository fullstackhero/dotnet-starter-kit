using MediatR;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using System.Text.Json.Serialization;
using System;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public class ChangePasswordCommand : IRequest<string>
{
    [JsonPropertyName("tcKimlikNo")]
    public string TcKimlikNo { get; set; } = string.Empty;
    
    [JsonPropertyName("currentPassword")]
    public string CurrentPasswordValue { get; set; } = string.Empty;
    
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
    
    [JsonPropertyName("confirmNewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
    
    // Domain validation method
    public bool IsValid() => 
        Tckn.IsValid(TcKimlikNo) && 
        !string.IsNullOrWhiteSpace(CurrentPasswordValue) &&
        Password.IsValid(NewPassword) &&
        string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal);
    
    // Get domain value objects
    public Tckn GetTcKimlik() => Tckn.CreateUnsafe(TcKimlikNo);
    public Password GetCurrentPassword() => Password.CreateUnsafe(CurrentPasswordValue);
    public Password GetPassword() => Password.CreateUnsafe(NewPassword);
}