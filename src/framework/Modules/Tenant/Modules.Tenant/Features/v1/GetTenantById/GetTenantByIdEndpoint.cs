using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Contracts.v1.GetTenantById;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Features.v1.GetTenantById;
public static class GetTenantByIdEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id}", (IQueryDispatcher dispatcher, string id)
            => dispatcher.SendAsync(new GetTenantByIdQuery(id)))
                                .WithName(nameof(GetTenantByIdEndpoint))
                                .WithSummary("get tenant by id")
                                .RequirePermission("Permissions.Tenants.View")
                                .WithDescription("get tenant by id");
    }
}