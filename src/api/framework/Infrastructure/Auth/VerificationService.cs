using System.Security.Cryptography;
using System.Text;
using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Core.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Auth;

public class VerificationService : IVerificationService
{
    private readonly ILogger<VerificationService> _logger;
    private readonly IMailService _mailService;
    private readonly ISmsService _smsService;
    private readonly IOptions<VerificationOptions> _options;

    public VerificationService(
        ILogger<VerificationService> logger,
        IMailService mailService,
        ISmsService smsService,
        IOptions<VerificationOptions> options)
    {
        _logger = logger;
        _mailService = mailService;
        _smsService = smsService;
        _options = options;
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(string email)
    {
        var token = await Task.Run(() => GenerateSecureToken());
        // TODO: Store token in database with expiration
        return token;
    }

    public async Task<string> GeneratePhoneVerificationTokenAsync(string phoneNumber)
    {
        var token = await _smsService.GenerateAndStoreSmsCodeAsync(phoneNumber);
        return token;
    }

    public async Task<bool> VerifyEmailAsync(string email, string token)
    {
        // TODO: Validate token from database
        return await Task.FromResult(true);
    }

    public async Task<bool> VerifyPhoneAsync(string phoneNumber, string token)
    {
        return await _smsService.ValidateSmsCodeAsync(phoneNumber, token);
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var verificationLink = new Uri(_options.Value.BaseUrl, $"/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}");
        
        var emailBody = $@"
            <h2>Email Doğrulama</h2>
            <p>Email adresinizi doğrulamak için aşağıdaki linke tıklayın:</p>
            <p><a href='{verificationLink}'>Email Adresimi Doğrula</a></p>
            <p>Bu link {_options.Value.EmailTokenExpirationHours} saat boyunca geçerlidir.</p>
            <p>Eğer bu işlemi siz yapmadıysanız, lütfen bu emaili dikkate almayın.</p>";

        var mailRequest = new MailRequest(
            to: new System.Collections.ObjectModel.Collection<string> { email },
            subject: "Email Doğrulama",
            body: emailBody);

        await _mailService.SendAsync(mailRequest, CancellationToken.None);
        _logger.LogInformation("Verification email sent to {Email}", email);
    }

    public async Task SendVerificationSmsAsync(string phoneNumber, string token)
    {
        await _smsService.SendSmsCodeAsync(phoneNumber, token);
        _logger.LogInformation("Verification SMS sent to {PhoneNumber}", phoneNumber);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

public class VerificationOptions
{
    public Uri BaseUrl { get; set; } = default!;
    public int EmailTokenExpirationHours { get; set; } = 24;
    public int PhoneTokenExpirationMinutes { get; set; } = 15;
} 