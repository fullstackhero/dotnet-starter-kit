namespace DN.WebApi.Shared.DTOs.Identity;

public class RoleDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Tenant { get; set; }
    public bool IsDefault { get; set; }
}