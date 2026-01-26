namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for user status and lifecycle operations.
/// </summary>
public interface IUserStatusService
{
    /// <summary>
    /// Toggles a user's active status.
    /// </summary>
    Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a user by deactivating them.
    /// </summary>
    Task DeleteAsync(string userId);
}
