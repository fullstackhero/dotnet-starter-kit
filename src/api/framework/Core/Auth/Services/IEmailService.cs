namespace FSH.Framework.Core.Auth.Services;

public interface IEmailService
{
    /// <summary>
    /// Şifre sıfırlama email'i gönderir
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    
    /// <summary>
    /// Genel email gönderme metodu
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body);
} 