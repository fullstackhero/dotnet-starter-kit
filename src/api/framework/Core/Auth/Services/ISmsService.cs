namespace FSH.Framework.Core.Auth.Services;

public interface ISmsService
{
    /// <summary>
    /// Telefon numarasına SMS kodu gönderir
    /// </summary>
    Task<bool> SendSmsCodeAsync(string phoneNumber, string code);
    
    /// <summary>
    /// SMS kodunun geçerli olup olmadığını kontrol eder
    /// </summary>
    Task<bool> ValidateSmsCodeAsync(string phoneNumber, string code);
    
    /// <summary>
    /// Yeni SMS kodu oluşturur ve saklarız
    /// </summary>
    Task<string> GenerateAndStoreSmsCodeAsync(string phoneNumber);
    
    /// <summary>
    /// Telefon numarasına özel SMS metni gönderir
    /// </summary>
    Task SendSmsAsync(string phoneNumber, string message);
} 