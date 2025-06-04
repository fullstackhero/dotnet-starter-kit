using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Auth;

internal sealed class ChangePasswordCommand
{
    [Required]
    public string CurrentPassword { get; init; } = default!;

    [Required]
    public string NewPassword { get; init; } = default!;
}
