using FSH.Framework.Core.Auth.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Services;

/// <summary>
/// Development SMS service that logs SMS messages instead of sending them
/// </summary>
public class DevelopmentSmsService : ISmsService
{
    private readonly ILogger<DevelopmentSmsService> _logger;

    public DevelopmentSmsService(ILogger<DevelopmentSmsService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendSmsCodeAsync(string phoneNumber, string code)
    {
        _logger.LogInformation("📱 SMS OTP Gönderildi (Development Mode)");
        _logger.LogInformation("📞 Telefon: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("🔐 OTP Kodu: {Code}", code);
        _logger.LogInformation("💬 Mesaj: 'Doğrulama kodunuz: {Code}. Bu kodu kimseyle paylaşmayın.'", code);
        
        return Task.FromResult(true);
    }

    public Task<bool> ValidateSmsCodeAsync(string phoneNumber, string code)
    {
        // Development mode'da her zaman true döner
        _logger.LogInformation("📱 SMS OTP Doğrulandı (Development Mode)");
        _logger.LogInformation("📞 Telefon: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("🔐 Kod: {Code}", code);
        
        return Task.FromResult(true);
    }

    public Task<string> GenerateAndStoreSmsCodeAsync(string phoneNumber)
    {
        var code = new Random().Next(1000, 9999).ToString();
        _logger.LogInformation("📱 SMS OTP Oluşturuldu (Development Mode)");
        _logger.LogInformation("📞 Telefon: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("🔐 Oluşturulan Kod: {Code}", code);
        
        return Task.FromResult(code);
    }

    public Task SendSmsAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("📱 SMS Gönderildi (Development Mode)");
        _logger.LogInformation("📞 Telefon: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("💬 Mesaj: {Message}", message);
        
        return Task.CompletedTask;
    }
} 