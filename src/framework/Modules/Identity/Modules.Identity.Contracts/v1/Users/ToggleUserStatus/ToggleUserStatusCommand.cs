namespace FSH.Framework.Identity.Endpoints.v1.Users.ToggleUserStatus;
public class ToggleUserStatusCommand
{
    public bool ActivateUser { get; set; }
    public string? UserId { get; set; }
}