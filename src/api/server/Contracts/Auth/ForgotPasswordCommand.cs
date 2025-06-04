using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Auth;

internal sealed class ForgotPasswordCommand
{
    [Required(ErrorMessage = "TC Kimlik numarası gereklidir")]
    [RegularExpression(@"^[1-9][0-9]{10}$", ErrorMessage = "Geçerli bir TC kimlik numarası giriniz (11 haneli)")]
    public string TcKimlikNo { get; init; } = string.Empty;
}

internal sealed class ValidateTcPhoneCommand
{
    [Required(ErrorMessage = "TC Kimlik numarası gereklidir")]
    [RegularExpression(@"^[1-9][0-9]{10}$", ErrorMessage = "Geçerli bir TC kimlik numarası giriniz (11 haneli)")]
    public string TcKimlikNo { get; init; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası gereklidir")]
    [RegularExpression(@"^(\+90|0)?[5][0-9]{9}$", ErrorMessage = "Geçerli bir Türkiye telefon numarası giriniz")]
    public string PhoneNumber { get; init; } = string.Empty;
}
