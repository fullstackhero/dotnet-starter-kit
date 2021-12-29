using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Controllers.Multitenancy;

[ApiController]
[Route("api/[controller]")]
[ApiVersionNeutral]
[ApiConventionType(typeof(FSHApiConventions))]
public class TenantsController : ControllerBase
{
    private readonly ITenantManager _tenantService;

    public TenantsController(ITenantManager tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("{key}")]
    [MustHavePermission(RootPermissions.Tenants.View)]
    [OpenApiOperation("Get Tenant Details.", "")]
    public async Task<ActionResult<Result<TenantDto>>> GetAsync(string key)
    {
        var tenant = await _tenantService.GetByKeyAsync(key);
        return Ok(tenant);
    }

    [HttpGet]
    [MustHavePermission(RootPermissions.Tenants.ListAll)]
    [OpenApiOperation("Get all the available Tenants.", "")]
    public async Task<ActionResult<Result<List<TenantDto>>>> GetAllAsync()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(tenants);
    }

    [HttpPost]
    [MustHavePermission(RootPermissions.Tenants.Create)]
    [OpenApiOperation("Create a new Tenant.", "")]
    public async Task<ActionResult<Result<Guid>>> CreateAsync(CreateTenantRequest request)
    {
        var tenantId = await _tenantService.CreateTenantAsync(request);
        return Ok(tenantId);
    }

    [HttpPost("upgrade")]
    [MustHavePermission(RootPermissions.Tenants.UpgradeSubscription)]
    [OpenApiOperation("Upgrade Subscription of Tenant.", "")]
    [ProducesResponseType(200)]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    public async Task<ActionResult<Result>> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request)
    {
        return Ok(await _tenantService.UpgradeSubscriptionAsync(request));
    }

    [HttpPost("{id}/deactivate")]
    [MustHavePermission(RootPermissions.Tenants.Update)]
    [OpenApiOperation("Deactivate Tenant.", "")]
    [ProducesResponseType(200)]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    public async Task<ActionResult<Result>> DeactivateTenantAsync(string id)
    {
        return Ok(await _tenantService.DeactivateTenantAsync(id));
    }

    [HttpPost("{id}/activate")]
    [MustHavePermission(RootPermissions.Tenants.Update)]
    [OpenApiOperation("Activate Tenant.", "")]
    [ProducesResponseType(200)]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    public async Task<ActionResult<Result>> ActivateTenantAsync(string id)
    {
        return Ok(await _tenantService.ActivateTenantAsync(id));
    }
}