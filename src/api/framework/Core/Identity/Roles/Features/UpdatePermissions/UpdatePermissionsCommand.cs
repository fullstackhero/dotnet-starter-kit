namespace FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;
public class UpdatePermissionsCommand
{
    public string RoleId { get; set; } = default!;
    public List<string> Permissions { get; set; } = default!;
}
