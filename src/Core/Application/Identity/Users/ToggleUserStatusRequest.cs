namespace FSH.WebAPI.Application.Identity.Users;

public class ToggleUserStatusRequest
{
    public bool ActivateUser { get; set; }
    public string? UserId { get; set; }
}
