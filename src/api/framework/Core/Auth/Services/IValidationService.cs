namespace FSH.Framework.Core.Auth.Services;

public interface IValidationService
{
    bool IsValidEmail(string email);
    bool IsValidPhoneNumber(string phoneNumber);
    bool IsValidTCKN(string tckn);
    bool IsValidUsername(string username);
    (bool IsValid, string ErrorMessage) ValidatePassword(string password);
} 