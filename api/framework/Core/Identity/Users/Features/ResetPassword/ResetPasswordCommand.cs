namespace FSH.Framework.Core.Identity.Users.Features.ResetPassword;
public class ResetPasswordCommand
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Token { get; set; }
}
