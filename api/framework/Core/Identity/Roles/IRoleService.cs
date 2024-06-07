using FSH.Framework.Core.Identity.Roles.Features;
using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using FSH.Framework.Core.Identity.Roles.Features.DeleteRole;

namespace FSH.Framework.Core.Identity.Roles;

public interface IRoleService
{
    Task<IEnumerable<RoleResponse>> GetAllRolesAsync();
    Task<RoleResponse?> GetRoleByIdAsync(string id);
    Task<RoleResponse> CreateOrUpdateRoleAsync(CreateOrUpdateRoleCommand command);
    Task DeleteRoleAsync(DeleteRoleCommand command);
}

