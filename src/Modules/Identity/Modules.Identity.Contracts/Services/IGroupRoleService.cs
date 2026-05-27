namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for retrieving roles derived from group memberships.
/// </summary>
public interface IGroupRoleService
{
    /// <summary>
    /// Gets all role names that a user has through their group memberships.
    /// </summary>
    /// <param name="userId">The user ID to get group roles for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of distinct role names from all groups the user belongs to.</returns>
    Task<IReadOnlyList<string>> GetUserGroupRolesAsync(string userId, CancellationToken ct = default);
}