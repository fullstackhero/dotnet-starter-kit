namespace DN.WebApi.Shared.DTOs.Identity;

public class ToggleUserStatusRequest
{
    public bool ActivateUser { get; set; }
    public string? UserId { get; set; }
}
