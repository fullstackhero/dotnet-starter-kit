using FSH.WebApi.Application.Multitenancy;

namespace FSH.WebApi.Host.Controllers.Multitenancy;

public class TenantsController : VersionNeutralApiController
{
    [HttpGet("{tenantId}")]
    [MustHavePermission(FSHRootPermissions.Tenants.View)]
    [OpenApiOperation("Get Tenant Details.", "")]
    public Task<TenantDto> GetAsync(string tenantId)
    {
        return Mediator.Send(new GetTenantRequest(tenantId));
    }

    [HttpGet]
    [MustHavePermission(FSHRootPermissions.Tenants.View)]
    [OpenApiOperation("Get all the available Tenants.", "")]
    public Task<List<TenantDto>> GetAllAsync()
    {
        return Mediator.Send(new GetAllTenantsRequest());
    }

    [HttpPost]
    [MustHavePermission(FSHRootPermissions.Tenants.Create)]
    [OpenApiOperation("Create a new Tenant.", "")]
    public Task<string> CreateAsync(CreateTenantRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("upgrade")]
    [MustHavePermission(FSHRootPermissions.Tenants.UpgradeSubscription)]
    [OpenApiOperation("Upgrade Subscription of Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("{tenantId}/deactivate")]
    [MustHavePermission(FSHRootPermissions.Tenants.Update)]
    [OpenApiOperation("Deactivate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> DeactivateTenantAsync(string tenantId)
    {
        return Mediator.Send(new DeactivateTenantRequest(tenantId));
    }

    [HttpPost("{tenantId}/activate")]
    [MustHavePermission(FSHRootPermissions.Tenants.Update)]
    [OpenApiOperation("Activate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> ActivateTenantAsync(string tenantId)
    {
        return Mediator.Send(new ActivateTenantRequest(tenantId));
    }
}