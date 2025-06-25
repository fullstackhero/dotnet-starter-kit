using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FSH.Starter.WebApi.Contracts.Auth;

internal sealed class ChangePasswordCommand
{
    [Required]
    [JsonPropertyName("tcKimlikNo")]
    public string TcKimlikNo { get; init; } = default!;

    [Required]
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; init; } = default!;

    [Required]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; init; } = default!;

    [Required]
    [JsonPropertyName("confirmNewPassword")]
    public string ConfirmNewPassword { get; init; } = default!;
}
