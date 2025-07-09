using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FSH.Starter.Tests.Unit")]

namespace FSH.Starter.WebApi.Contracts.Auth;

internal sealed class ResetPasswordCommand
{
    [Required(ErrorMessage = "TC Kimlik numarası gereklidir")]
    [RegularExpression(@"^[1-9][0-9]{10}$", ErrorMessage = "Geçerli bir TC kimlik numarası giriniz (11 haneli)")]
    public string TcKimlikNo { get; init; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası gereklidir")]
    [RegularExpression(@"^(\+90|0)?[5][0-9]{9}$", ErrorMessage = "Geçerli bir Türkiye telefon numarası giriniz")]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "SMS kodu gereklidir")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "SMS kodu 6 haneli sayı olmalıdır")]
    public string SmsCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre gereklidir")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Şifre en az 1 küçük harf, 1 büyük harf, 1 rakam ve 1 özel karakter içermelidir")]
    public string NewPassword { get; init; } = string.Empty;
}
