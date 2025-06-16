using FSH.Framework.Core.Auth.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Auth;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        try
        {
            // TODO: Implement actual email sending logic
            // This is a placeholder implementation
            _logger.LogInformation("Sending password reset email to: {Email} with link: {ResetLink}", email, resetLink);
            
            var subject = "Şifre Sıfırlama Talebi";
            var body = $@"
                <html>
                <body>
                    <h2>Şifre Sıfırlama</h2>
                    <p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayınız:</p>
                    <p><a href='{resetLink}'>Şifremi Sıfırla</a></p>
                    <p>Bu bağlantı 15 dakika süreyle geçerlidir.</p>
                    <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to: {Email}", email);
            throw;
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            // TODO: Integrate with actual email service (SendGrid, SMTP, etc.)
            // For now, just log the email details
            _logger.LogInformation("EMAIL SENT - To: {To}, Subject: {Subject}", to, subject);
            _logger.LogDebug("EMAIL BODY: {Body}", body);
            
            // Simulate async operation
            await Task.Delay(100);
            
            _logger.LogInformation("Email successfully sent to: {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {To}", to);
            throw;
        }
    }
} 