using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Controllers.Multitenancy;

public class TenantsController : VersionNeutralApiController
{
    private readonly ITenantManager _tenantService;

    public TenantsController(ITenantManager tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("{key}")]
    [MustHavePermission(FSHRootPermissions.Tenants.View)]
    [OpenApiOperation("Get Tenant Details.", "")]
    public async Task<ActionResult<Result<TenantDto>>> GetAsync(string key)
    {
        var tenant = await _tenantService.GetByKeyAsync(key);
        return Ok(tenant);
    }

    [HttpGet]
    [MustHavePermission(FSHRootPermissions.Tenants.ListAll)]
    [OpenApiOperation("Get all the available Tenants.", "")]
    public async Task<ActionResult<Result<List<TenantDto>>>> GetAllAsync()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(tenants);
    }

    [HttpPost]
    [MustHavePermission(FSHRootPermissions.Tenants.Create)]
    [OpenApiOperation("Create a new Tenant.", "")]
    public async Task<ActionResult<Result<Guid>>> CreateAsync(CreateTenantRequest request)
    {
        var tenantId = await _tenantService.CreateTenantAsync(request);
        return Ok(tenantId);
    }

    [HttpPost("upgrade")]
    [MustHavePermission(FSHRootPermissions.Tenants.UpgradeSubscription)]
    [OpenApiOperation("Upgrade Subscription of Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<ActionResult<Result>> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request)
    {
        return Ok(await _tenantService.UpgradeSubscriptionAsync(request));
    }

    [HttpPost("{id}/deactivate")]
    [MustHavePermission(FSHRootPermissions.Tenants.Update)]
    [OpenApiOperation("Deactivate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<ActionResult<Result>> DeactivateTenantAsync(string id)
    {
        return Ok(await _tenantService.DeactivateTenantAsync(id));
    }

    [HttpPost("{id}/activate")]
    [MustHavePermission(FSHRootPermissions.Tenants.Update)]
    [OpenApiOperation("Activate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<ActionResult<Result>> ActivateTenantAsync(string id)
    {
        return Ok(await _tenantService.ActivateTenantAsync(id));
    }
}