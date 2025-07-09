using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("FSH.Starter.Tests.Unit")]

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
