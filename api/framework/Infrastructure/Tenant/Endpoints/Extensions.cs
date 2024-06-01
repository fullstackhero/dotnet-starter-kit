using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Tenant.Endpoints;
public static class Extensions
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var tenantGroup = app.MapGroup("api/tenants").WithTags("tenants");
        tenantGroup.MapRegisterTenantEndpoint();
        tenantGroup.MapGetTenantsEndpoint();
        tenantGroup.MapGetTenantByIdEndpoint();
        tenantGroup.MapUpgradeTenantSubscriptionEndpoint();
        tenantGroup.MapActivateTenantEndpoint();
        tenantGroup.MapDisableTenantEndpoint();
        return app;
    }
}
