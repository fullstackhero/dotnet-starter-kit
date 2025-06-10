using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
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
    
    // In-memory storage for email tokens (production'da Redis kullanılmalı)
    private static readonly ConcurrentDictionary<string, EmailTokenInfo> _emailTokens = new();

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
        
        // Token'ı kaydet (config'ten süreyi al)
        var expirationMinutes = _options.Value.EmailTokenExpirationMinutes;
        var tokenInfo = new EmailTokenInfo(token, DateTime.UtcNow.AddMinutes(expirationMinutes));
        _emailTokens.AddOrUpdate(email, tokenInfo, (key, oldValue) => tokenInfo);
        
        _logger.LogInformation("Email verification token generated for {Email}, expires in {Minutes} minutes", email, expirationMinutes);
        
        // Development: Log token for testing
        _logger.LogWarning("🔧 DEV: Email verification token for {Email}: {Token}", email, token);
        
        return token;
    }

    public async Task<string> GeneratePhoneVerificationTokenAsync(string phoneNumber)
    {
        var token = await _smsService.GenerateAndStoreSmsCodeAsync(phoneNumber);
        return token;
    }

    public async Task<bool> VerifyEmailAsync(string email, string token)
    {
        await Task.CompletedTask;
        
        if (!_emailTokens.TryGetValue(email, out var tokenInfo))
        {
            _logger.LogWarning("No email verification token found for: {Email}", email);
            return false;
        }

        // Token süresi dolmuş mu kontrol et
        if (DateTime.UtcNow > tokenInfo.ExpiresAt)
        {
            _emailTokens.TryRemove(email, out _);
            _logger.LogWarning("Email verification token expired for: {Email}", email);
            return false;
        }

        // Token eşleşiyor mu?
        if (tokenInfo.Token != token)
        {
            _logger.LogWarning("Invalid email verification token for: {Email}", email);
            return false;
        }

        // Kullanıldıktan sonra sil
        _emailTokens.TryRemove(email, out _);
        _logger.LogInformation("Email verification successful for: {Email}", email);
        return true;
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
            <p>Bu link {_options.Value.EmailTokenExpirationMinutes} dakika boyunca geçerlidir.</p>
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

    private sealed record EmailTokenInfo(string Token, DateTime ExpiresAt);
}

public class VerificationOptions
{
    public Uri BaseUrl { get; set; } = default!;
    public int EmailTokenExpirationMinutes { get; set; } = 60;
    public int PhoneTokenExpirationMinutes { get; set; } = 15;
} 