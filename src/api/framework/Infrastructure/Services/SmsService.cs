using System.Collections.Concurrent;
using FSH.Framework.Core.Auth.Services;
using System.Security.Cryptography;

namespace FSH.Framework.Infrastructure.Services;

public class SmsService : ISmsService
{
    // In-memory storage for SMS codes (production'da Redis kullanılmalı)
    private static readonly ConcurrentDictionary<string, SmsCodeInfo> _smsCodes = new();

    public async Task<bool> SendSmsCodeAsync(string phoneNumber, string code)
    {
        // TODO: Gerçek SMS API entegrasyonu (Netgsm, Iletimerkezi, vb.)
        // Şimdilik console'a yazdırıyoruz
        Console.WriteLine($"SMS Code sent to {phoneNumber}: {code}");
        
        // Simulated delay
        await Task.Delay(100);
        
        return true; // SMS gönderimini başarılı kabul ediyoruz
    }

    public async Task<bool> ValidateSmsCodeAsync(string phoneNumber, string code)
    {
        await Task.CompletedTask;
        
        if (!_smsCodes.TryGetValue(phoneNumber, out var smsInfo))
            return false;

        // Kod süresi dolmuş mu kontrol et (5 dakika)
        if (DateTime.UtcNow > smsInfo.ExpiresAt)
        {
            _smsCodes.TryRemove(phoneNumber, out _);
            return false;
        }

        // Kod eşleşiyor mu?
        if (smsInfo.Code != code)
            return false;

        // Kullanıldıktan sonra sil
        _smsCodes.TryRemove(phoneNumber, out _);
        return true;
    }

    public async Task<string> GenerateAndStoreSmsCodeAsync(string phoneNumber)
    {
        // 6 haneli rastgele kod oluştur
        var code = GenerateSmsCode();

        // Kodu kaydet (5 dakika geçerli)
        var smsInfo = new SmsCodeInfo(code, DateTime.UtcNow.AddMinutes(5));
        _smsCodes.AddOrUpdate(phoneNumber, smsInfo, (key, oldValue) => smsInfo);

        // SMS gönder
        await SendSmsCodeAsync(phoneNumber, code);

        return code;
    }

    private static string GenerateSmsCode()
    {
        var randomBytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var code = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % 1000000;
        return code.ToString("D6");
    }

    private sealed record SmsCodeInfo(string Code, DateTime ExpiresAt);
} 