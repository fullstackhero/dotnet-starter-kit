namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users;

public class ToggleUserStatusRequest
{
    public bool ActivateUser { get; set; }
    public string? UserId { get; set; }
}
