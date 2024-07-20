namespace FSH.Framework.Core.Identity.Users.Features.ToggleUserStatus;
public class ToggleUserStatusCommand
{
    public bool ActivateUser { get; set; }
    public string? UserId { get; set; }
}
