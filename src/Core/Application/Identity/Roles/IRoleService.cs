namespace FSH.WebApi.Application.Identity.Roles;

public interface IRoleService : ITransientService
{
    Task<List<RoleDto>> GetListAsync();

    Task<List<PermissionDto>> GetPermissionsByRoleAsync(string roleId, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<RoleDto> GetByIdAsync(string id);

    Task<bool> ExistsAsync(string roleName, string? excludeId);

    Task<string> RegisterRoleAsync(RoleRequest request);

    Task<string> DeleteAsync(string id);

    Task<List<RoleDto>> GetUserRolesAsync(string userId);

    Task<string> UpdatePermissionsByRoleAsync(string roleId, List<UpdatePermissionsRequest> request, CancellationToken cancellationToken);
}