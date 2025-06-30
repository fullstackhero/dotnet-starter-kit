using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Mail;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Core.Auth.Events;

public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredEventHandler> _logger;
    private readonly IMailService _mailService;
    private readonly IVerificationService _verificationService;

    public UserRegisteredEventHandler(
        ILogger<UserRegisteredEventHandler> logger,
        IMailService mailService,
        IVerificationService verificationService)
    {
        _logger = logger;
        _mailService = mailService;
        _verificationService = verificationService;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "User registered: {UserId}, Email: {Email}, TCKN: {Tckn}, Name: {FirstName} {LastName}",
                notification.UserId,
                notification.Email,
                notification.Tckn,
                notification.FirstName,
                notification.LastName);

            // Generate email verification token
            var verificationToken = await _verificationService.GenerateEmailVerificationTokenAsync(notification.Email);

            // Send verification email
            await _verificationService.SendVerificationEmailAsync(notification.Email, verificationToken);

            // Send welcome email
            var welcomeEmailBody = $@"
                <h2>Hoş Geldiniz {notification.FirstName} {notification.LastName}!</h2>
                <p>FSH Framework'e kayıt olduğunuz için teşekkür ederiz.</p>
                <p>Hesabınızı aktifleştirmek için lütfen email adresinizi doğrulayın.</p>
                <p>Herhangi bir sorunuz olursa bizimle iletişime geçmekten çekinmeyin.</p>
                <p>Saygılarımızla,<br>FSH Framework Ekibi</p>";

            var welcomeEmail = new MailRequest(
                to: new System.Collections.ObjectModel.Collection<string> { notification.Email },
                subject: "FSH Framework'e Hoş Geldiniz!",
                body: welcomeEmailBody);

            await _mailService.SendAsync(welcomeEmail, cancellationToken);

            _logger.LogInformation("Welcome and verification emails sent to user: {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emails to user: {UserId}", notification.UserId);
            // We don't throw here to prevent the registration process from failing
            // The user can request a new verification email later
        }
    }
}