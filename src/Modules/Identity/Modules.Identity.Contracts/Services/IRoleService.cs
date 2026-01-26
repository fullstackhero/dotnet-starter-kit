using FSH.Modules.Identity.Contracts.DTOs;

namespace FSH.Modules.Identity.Contracts.Services;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDto?> GetRoleAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateOrUpdateRoleAsync(string roleId, string name, string description, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken = default);
    Task<string> UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default);
}
