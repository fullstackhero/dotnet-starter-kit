using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Auth;

/// <summary>
/// Login request DTO for the presentation layer.
/// Follows Clean Architecture by separating external contracts from domain models.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Turkish Citizen ID (TC Kimlik No) or Member Number
    /// </summary>
    [Required(ErrorMessage = "TC Kimlik No veya Üye No gereklidir")]
    public string TcknOrMemberNumber { get; init; } = default!;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Şifre gereklidir")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    public string Password { get; init; } = default!;
} 