using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Auth;

/// <summary>
/// OTP verification request for completing registration
/// </summary>
public sealed class VerifyRegistrationRequest
{
    /// <summary>
    /// Phone number (Turkish format)
    /// </summary>
    [Required(ErrorMessage = "Telefon numarası gereklidir")]
    [RegularExpression(@"^5\d{2}(\s?\d{3}\s?\d{2}\s?\d{2}|\d{7})$", ErrorMessage = "Telefon numarası 5XXXXXXXXX veya 5XX XXX XX XX formatında olmalıdır")]
    public string PhoneNumber { get; init; } = default!;

    /// <summary>
    /// 4-digit OTP code sent via SMS
    /// </summary>
    [Required(ErrorMessage = "Doğrulama kodu gereklidir")]
    [StringLength(4, MinimumLength = 4, ErrorMessage = "Doğrulama kodu 4 haneli olmalıdır")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Doğrulama kodu sadece rakamlardan oluşmalıdır")]
    public string OtpCode { get; init; } = default!;
} 