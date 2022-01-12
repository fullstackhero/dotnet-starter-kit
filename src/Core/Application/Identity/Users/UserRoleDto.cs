namespace FSH.WebApi.Application.Identity.Users;

public class UserRoleDto
{
    public string? RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool Enabled { get; set; }
}