using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Contracts.v1.UpgradeTenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Features.v1.UpgradeTenant;
public static class UpgradeTenantEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/upgrade", ([FromBody] UpgradeTenantCommand command, ICommandDispatcher dispatcher)
            => dispatcher.SendAsync(command))
                            .WithName(nameof(UpgradeTenantEndpoint))
                            .WithSummary("upgrade tenant subscription")
                            .RequirePermission("Permissions.Tenants.Update")
                            .WithDescription("upgrade tenant subscription");
    }
}