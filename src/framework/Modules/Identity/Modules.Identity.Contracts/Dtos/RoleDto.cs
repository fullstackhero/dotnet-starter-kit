namespace FSH.Framework.Identity.Core.Roles;

public class RoleDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public IReadOnlyCollection<string>? Permissions { get; set; }
}