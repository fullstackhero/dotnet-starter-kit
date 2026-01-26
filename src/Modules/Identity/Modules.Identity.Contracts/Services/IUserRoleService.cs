using FSH.Modules.Identity.Contracts.DTOs;

namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service for user role management.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Assigns roles to a user.
    /// </summary>
    Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all roles for a user.
    /// </summary>
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);
}
