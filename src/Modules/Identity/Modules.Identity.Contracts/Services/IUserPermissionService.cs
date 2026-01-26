namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for user permission operations.
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Gets all permissions for a user.
    /// </summary>
    Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the permission cache for a user.
    /// </summary>
    Task InvalidatePermissionCacheAsync(string userId, CancellationToken cancellationToken);
}
