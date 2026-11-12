namespace FSH.Modules.Identity.Authorization;

/// <summary>
/// Startup reconciliation of the built-in roles' permission claims (config section
/// <c>"PermissionSyncOptions"</c>), run by <see cref="RolePermissionSyncer"/>.
/// </summary>
public sealed class PermissionSyncOptions
{
    public const string SectionName = "PermissionSyncOptions";

    /// <summary>
    /// When <c>true</c> (the default), the sync also removes permission claims the catalog no longer
    /// grants. Set it to <c>false</c> on any host that registers <c>IdentityModule</c> without every
    /// module the API loads: its catalog is partial, so removal would revoke the missing modules'
    /// permissions from the built-in roles in every tenant. Missing permissions are still added.
    /// </summary>
    public bool RemoveRevoked { get; set; } = true;
}
