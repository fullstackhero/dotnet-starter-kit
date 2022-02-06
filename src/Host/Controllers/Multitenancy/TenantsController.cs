using FSH.WebApi.Application.Multitenancy;

namespace FSH.WebApi.Host.Controllers.Multitenancy;

public class TenantsController : VersionNeutralApiController
{
    [HttpGet]
    [MustHavePermission(FSHAction.View, FSHResource.Tenants)]
    [OpenApiOperation("Get all the available Tenants.", "")]
    public Task<List<TenantDto>> GetListAsync()
    {
        return Mediator.Send(new GetAllTenantsRequest());
    }

    [HttpGet("{tenantId}")]
    [MustHavePermission(FSHAction.View, FSHResource.Tenants)]
    [OpenApiOperation("Get Tenant Details.", "")]
    public Task<TenantDto> GetAsync(string tenantId)
    {
        return Mediator.Send(new GetTenantRequest(tenantId));
    }

    [HttpPost]
    [MustHavePermission(FSHAction.Create, FSHResource.Tenants)]
    [OpenApiOperation("Create a new Tenant.", "")]
    public Task<string> CreateAsync(CreateTenantRequest request)
    {
        return Mediator.Send(request);
    }

    [HttpPost("{tenantId}/activate")]
    [MustHavePermission(FSHAction.Update, FSHResource.Tenants)]
    [OpenApiOperation("Activate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> ActivateAsync(string tenantId)
    {
        return Mediator.Send(new ActivateTenantRequest(tenantId));
    }

    [HttpPost("{tenantId}/deactivate")]
    [MustHavePermission(FSHAction.Update, FSHResource.Tenants)]
    [OpenApiOperation("Deactivate Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> DeactivateAsync(string tenantId)
    {
        return Mediator.Send(new DeactivateTenantRequest(tenantId));
    }

    [HttpPost("{tenantId}/upgrade")]
    [MustHavePermission(FSHAction.UpgradeSubscription, FSHResource.Tenants)]
    [OpenApiOperation("Upgrade Subscription of Tenant.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public async Task<ActionResult<string>> UpgradeSubscriptionAsync(string tenantId, UpgradeSubscriptionRequest request)
    {
        return tenantId != request.TenantId
            ? BadRequest()
            : Ok(await Mediator.Send(request));
    }
}