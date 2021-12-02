using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Multitenancy;

namespace DN.WebApi.Application.Multitenancy;

public interface ITenantManager : ITransientService
{
    public Task<Result<TenantDto>> GetByKeyAsync(string key);

    public Task<Result<List<TenantDto>>> GetAllAsync();

    public Task<Result<Guid>> CreateTenantAsync(CreateTenantRequest request);

    Task<Result<string>> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request);

    Task<Result<string>> DeactivateTenantAsync(string tenant);

    Task<Result<string>> ActivateTenantAsync(string tenant);

    Result<IEnumerable<string>> GetAllBannedIp();

    Result<bool> UnBanIp(string ipAddress);
}