using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Multitenancy;
using GrpcShared.Controllers;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Multitenancy;

public class TenantsControllerGrpc : ITenantsControllerGrpc
{
    private readonly ITenantManager _tenantService;

    public TenantsControllerGrpc(ITenantManager tenantService)
    {
        _tenantService = tenantService;
    }

    [MustHavePermission(RootPermissions.Tenants.View)]
    public async Task<Result<TenantDto>> GetAsync(string key, CallContext context)
    {
        var result = await _tenantService.GetByKeyAsync(key);
        return result;
    }

    [MustHavePermission(RootPermissions.Tenants.ListAll)]
    public async Task<Result<List<TenantDto>>> GetAllAsync(CallContext context)
    {
        var result = await _tenantService.GetAllAsync();
        return result;
    }

    [MustHavePermission(RootPermissions.Tenants.Create)]
    public async Task<Result<Guid>> CreateAsync(CreateTenantRequest request, CallContext context)
    {
        var result = await _tenantService.CreateTenantAsync(request);
        return result;
    }

    [MustHavePermission(RootPermissions.Tenants.UpgradeSubscription)]
    public async Task<Result<string>> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request, CallContext context)
    {
        var result = await _tenantService.UpgradeSubscriptionAsync(request);
        return result;
    }

    [MustHavePermission(RootPermissions.Tenants.Update)]
    public async Task<Result<string>> DeactivateTenantAsync(string tenant, CallContext context)
    {
        var result = await _tenantService.DeactivateTenantAsync(tenant);
        return result;
    }

    [MustHavePermission(RootPermissions.Tenants.Update)]
    public async Task<Result<string>> ActivateTenantAsync(string tenant, CallContext context)
    {
        var result = await _tenantService.ActivateTenantAsync(tenant);
        return result;
    }

    [MustHavePermission(RootPermissions.Tenants.ListAll)]
    public Result<IEnumerable<string>> GetAllBannedIp(CallContext context)
    {
        return _tenantService.GetAllBannedIp();
    }

    [MustHavePermission(RootPermissions.Tenants.ListAll)]
    public Result<bool> UnBanIp(string ipAddress, CallContext context)
    {
        return _tenantService.UnBanIp(ipAddress);
    }
}
