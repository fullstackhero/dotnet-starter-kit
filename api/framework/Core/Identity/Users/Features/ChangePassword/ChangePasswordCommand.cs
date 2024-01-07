namespace FSH.Framework.Core.Identity.Users.Features.ChangePassword;
public class ChangePasswordCommand
{
    public string Password { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmNewPassword { get; set; } = default!;
}
