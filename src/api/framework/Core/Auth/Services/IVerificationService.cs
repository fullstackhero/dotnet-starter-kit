using System.Threading.Tasks;

namespace FSH.Framework.Core.Auth.Services;

public interface IVerificationService
{
    Task<string> GenerateEmailVerificationTokenAsync(string email);
    Task<string> GeneratePhoneVerificationTokenAsync(string phoneNumber);
    Task<bool> VerifyEmailAsync(string email, string token);
    Task<bool> VerifyPhoneAsync(string phoneNumber, string token);
    Task SendVerificationEmailAsync(string email, string token);
    Task SendVerificationSmsAsync(string phoneNumber, string token);
}