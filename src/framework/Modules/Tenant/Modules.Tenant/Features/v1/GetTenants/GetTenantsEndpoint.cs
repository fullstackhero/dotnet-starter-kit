using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Contracts.v1.GetTenants;
using FSH.Modules.Tenant.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Features.v1.GetTenants;
public static class GetTenantsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (IQueryDispatcher dispatcher)
            => dispatcher.SendAsync(new GetTenantsQuery()))
                                .WithName(nameof(GetTenantsEndpoint))
                                .HasApiVersion(1)
                                .WithSummary("get tenants")
                                .RequirePermission(TenantConstants.Permissions.View)
                                .WithDescription("get tenants");
    }
}