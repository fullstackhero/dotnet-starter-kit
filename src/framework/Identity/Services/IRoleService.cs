using FSH.Framework.Identity.Endpoints.v1.Roles.CreateOrUpdateRole;
using FSH.Framework.Identity.Endpoints.v1.Roles.UpdatePermissions;

namespace FSH.Framework.Identity.Core.Roles;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<RoleDto?> GetRoleAsync(string id);
    Task<RoleDto> CreateOrUpdateRoleAsync(UpsertRoleCommand request);
    Task DeleteRoleAsync(string id);
    Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken);

    Task<string> UpdatePermissionsAsync(UpdatePermissionsCommand request);
}

