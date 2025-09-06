using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Contracts.v1.ActivateTenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tenant.Features.v1.ActivateTenant;
public static class ActivateTenantEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/activate", async ([FromServices] ICommandDispatcher dispatcher, string id)
            => await dispatcher.SendAsync(new ActivateTenantCommand(id)))
                                .WithName(nameof(ActivateTenantEndpoint))
                                .WithSummary("Activate Tenant")
                                .RequirePermission("Permissions.Tenants.Update")
                                .WithDescription("Activate Tenant");
    }
}