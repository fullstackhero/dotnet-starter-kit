namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users.Password;

public class ResetPasswordRequest
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Token { get; set; }
}