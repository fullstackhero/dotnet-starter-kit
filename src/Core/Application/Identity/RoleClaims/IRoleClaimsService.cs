namespace FSH.WebApi.Application.Identity.RoleClaims;

public interface IRoleClaimsService : ITransientService
{
    public Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    Task<List<RoleClaimDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<RoleClaimDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<List<RoleClaimDto>> GetAllByRoleIdAsync(string roleId, CancellationToken cancellationToken);

    Task<string> SaveAsync(RoleClaimRequest request, CancellationToken cancellationToken);

    Task<string> DeleteAsync(int id, CancellationToken cancellationToken);
}