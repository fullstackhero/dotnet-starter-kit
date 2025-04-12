namespace FSH.Framework.Identity.Endpoints.v1.Roles.UpdatePermissions;

public class UpdatePermissionsCommand
{
    /// <summary>
    /// The ID of the role to update.
    /// </summary>
    public string RoleId { get; init; } = default!;

    /// <summary>
    /// The list of permissions to assign to the role.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
