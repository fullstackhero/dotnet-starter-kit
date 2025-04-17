namespace FSH.Framework.Modules.Identity.Contracts.v1.Roles.UpdatePermissions;

public class UpdatePermissionsCommand
{
    /// <summary>
    /// The ID of the role to update.
    /// </summary>
    public string RoleId { get; init; } = default!;

    /// <summary>
    /// The list of permissions to assign to the role.
    /// </summary>
    public List<string> Permissions { get; init; } = [];
}