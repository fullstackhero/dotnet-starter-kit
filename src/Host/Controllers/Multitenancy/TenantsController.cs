using FSH.WebApi.Application.Multitenancy;

namespace FSH.WebApi.Host.Controllers.Multitenancy;

public class TenantsController : VersionNeutralApiController
{
    [HttpGet("{tenantKey}")]
    [MustHavePermission(FSHRootPermissions.Tenants.View)]
    [OpenApiOperation("Get Tenant Details.", "")]
    public Task<TenantDto> GetAsync(string tenantKey)
    {
        return Mediator.Send(new GetTenantByKeyRequest(tenantKey));
    }

    [HttpGet]
    [MustHavePermission(FSHRootPermissions.Tenants.ListAll)]
    [OpenApiOperation("Get all the available Tenants.", "")]
    public Task<List<TenantDto>> GetAllAsync()
    {
        return Mediator.Send(new GetAllTenantsRequest());
    }

    [HttpPost]
    [MustHavePermission(FSHRootPermissions.Tenants.Create)]
    [OpenApiOperation("Create a new Tenant.", "")]
    public Task<Guid> CreateAsync(CreateTenantRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("upgrade")]
    [MustHavePermission(FSHRootPermissions.Tenants.UpgradeSubscription)]
    [OpenApiOperation("Upgrade Subscription of Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public Task<string> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("{tenantKey}/deactivate")]
    [MustHavePermission(FSHRootPermissions.Tenants.Update)]
    [OpenApiOperation("Deactivate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public Task<string> DeactivateTenantAsync(string tenantKey)
    {
        return Mediator.Send(new DeactivateTenantRequest(tenantKey));
    }

    [HttpPost("{tenantKey}/activate")]
    [MustHavePermission(FSHRootPermissions.Tenants.Update)]
    [OpenApiOperation("Activate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public Task<string> ActivateTenantAsync(string tenantKey)
    {
        return Mediator.Send(new ActivateTenantRequest(tenantKey));
    }
}