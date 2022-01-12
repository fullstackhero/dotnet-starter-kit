namespace FSH.WebAPI.Application.Identity.Users;

public class UserRolesRequest
{
    public List<UserRoleDto> UserRoles { get; set; } = new();
}