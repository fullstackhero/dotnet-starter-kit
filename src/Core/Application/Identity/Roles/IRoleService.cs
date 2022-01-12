namespace FSH.WebApi.Application.Identity.Roles;

public interface IRoleService : ITransientService
{
    Task<List<RoleDto>> GetListAsync();

    Task<List<PermissionDto>> GetPermissionsAsync(string id, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<RoleDto> GetByIdAsync(string id);

    Task<bool> ExistsAsync(string roleName, string? excludeId);

    Task<string> RegisterRoleAsync(RoleRequest request);

    Task<string> DeleteAsync(string id);

    Task<List<RoleDto>> GetUserRolesAsync(string userId);

    Task<string> UpdatePermissionsAsync(string id, List<UpdatePermissionsRequest> request, CancellationToken cancellationToken);
}