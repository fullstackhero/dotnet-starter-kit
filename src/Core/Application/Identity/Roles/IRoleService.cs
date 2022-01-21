namespace FSH.WebApi.Application.Identity.Roles;

public interface IRoleService : ITransientService
{
    Task<List<RoleDto>> GetListAsync();

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string roleName, string? excludeId);

    Task<RoleDto> GetByIdAsync(string id);

    Task<RoleDto> GetByIdWithPermissionsAsync(string roleId, CancellationToken cancellationToken);

    Task<string> RegisterRoleAsync(RoleRequest request);

    Task<string> UpdatePermissionsAsync(UpdatePermissionsRequest request, CancellationToken cancellationToken);

    Task<string> DeleteAsync(string id);
}