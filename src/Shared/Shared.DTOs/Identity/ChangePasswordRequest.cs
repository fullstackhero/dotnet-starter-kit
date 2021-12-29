namespace DN.WebApi.Shared.DTOs.Identity;

public class ChangePasswordRequest
{
    public string Password { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmNewPassword { get; set; } = default!;
}