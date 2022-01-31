namespace FSH.WebApi.Application.Identity.Roles;

public class RoleDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsRootRole { get; set; } = false;
    public List<PermissionDto>? Permissions { get; set; }
}