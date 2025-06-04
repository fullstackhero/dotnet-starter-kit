using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Auth;

/// <summary>
/// Login request DTO for the presentation layer.
/// Follows Clean Architecture by separating external contracts from domain models.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Turkish Citizen ID (TC Kimlik No)
    /// </summary>
    [Required(ErrorMessage = "TC Kimlik No gereklidir")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "TC Kimlik No 11 haneli olmalıdır")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "TC Kimlik No sadece rakamlardan oluşmalıdır")]
    public string Tckn { get; init; } = default!;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Şifre gereklidir")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    public string Password { get; init; } = default!;
} 