namespace FSH.Framework.Core.Auth.Services;

public interface IPasswordResetService
{
    Task<string> GenerateResetTokenAsync(string email);
    Task<bool> ValidateResetTokenAsync(string email, string token);
    Task<bool> ValidateResetTokenAsync(string token); // Token-only validation
    Task<string?> GetTcknFromTokenAsync(string token); // Get TCKN associated with token
    Task InvalidateResetTokenAsync(string email);
    Task<bool> IsRateLimitedAsync(string email);
    Task ResetUserPasswordAsync(string email, string newPassword);
    Task ResetUserPasswordByTcknAsync(string tcKimlik, string newPassword);
} 