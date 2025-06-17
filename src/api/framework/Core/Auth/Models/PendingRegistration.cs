namespace FSH.Framework.Core.Auth.Models;

public class PendingRegistrationData
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Tckn { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ProfessionId { get; set; }
    public DateTime? BirthDate { get; set; }
}

public class PendingRegistration
{
    public string PhoneNumber { get; set; } = string.Empty;
    public PendingRegistrationData RegistrationData { get; set; } = new();
    public string OtpCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; } = 0;
    public string RegistrationIp { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool HasExceededMaxAttempts => Attempts >= 3;
    
    public static PendingRegistration Create(PendingRegistrationData data, string registrationIp, string deviceInfo)
    {
        return new PendingRegistration
        {
            PhoneNumber = data.PhoneNumber,
            RegistrationData = data,
            OtpCode = GenerateOtpCode(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RegistrationIp = registrationIp,
            DeviceInfo = deviceInfo,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void RegenerateOtp()
    {
        OtpCode = GenerateOtpCode();
        ExpiresAt = DateTime.UtcNow.AddMinutes(15);
        Attempts = 0;
    }
    
    public bool ValidateOtp(string providedOtp)
    {
        Attempts++;
        return !IsExpired && !HasExceededMaxAttempts && OtpCode == providedOtp;
    }
    
    private static string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(1000, 9999).ToString();
    }
} 