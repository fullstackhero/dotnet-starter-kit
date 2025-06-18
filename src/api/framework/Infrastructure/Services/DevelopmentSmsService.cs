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
        _logger.LogWarning("=== 📱 SMS OTP GÖNDERİLDİ (DEVELOPMENT MODE) ===");
        _logger.LogWarning("📞 Telefon: {PhoneNumber}", phoneNumber);
        _logger.LogWarning("🔐 OTP KODU: {Code}", code);
        _logger.LogWarning("💬 Mesaj: 'Doğrulama kodunuz: {Code}. Bu kodu kimseyle paylaşmayın.'", code);
        _logger.LogWarning("=============================================");
        
        // Console'a da yazdır (daha görünür olması için)
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=== 📱 SMS OTP GÖNDERİLDİ ===");
        Console.WriteLine($"📞 Telefon: {phoneNumber}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"🔐 OTP KODU: {code}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"💬 Mesaj: 'Doğrulama kodunuz: {code}. Bu kodu kimseyle paylaşmayın.'");
        Console.WriteLine("===========================");
        Console.ResetColor();
        
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