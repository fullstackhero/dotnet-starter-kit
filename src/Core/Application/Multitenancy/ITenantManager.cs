using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Multitenancy;

namespace DN.WebApi.Application.Multitenancy;

public interface ITenantManager : ITransientService
{
    public Task<Result<TenantDto>> GetByKeyAsync(string key);

    public Task<Result<TenantDto>> GetByIssuerAsync(string issuer);

    public Task<Result<List<TenantDto>>> GetAllAsync();

    public Task<Result<Guid>> CreateTenantAsync(CreateTenantRequest request);

    Task<IResult> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request);

    Task<IResult> DeactivateTenantAsync(string tenant);

    Task<IResult> ActivateTenantAsync(string tenant);
}