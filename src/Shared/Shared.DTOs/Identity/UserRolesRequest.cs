namespace DN.WebApi.Shared.DTOs.Identity;

public class UserRolesRequest
{
    public List<UserRoleDto> UserRoles { get; set; } = new();
}