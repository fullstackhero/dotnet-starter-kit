using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Endpoints.v1.Activate;
public static partial class ActivateTenant
{
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/activate", async (ICommandDispatcher dispatcher, string id)
            => await dispatcher.SendAsync<Command, Response>(new Command(id)))
                                .WithName(nameof(ActivateTenant))
                                .WithSummary("activate tenant")
                                .RequirePermission("Permissions.Tenants.Update")
                                .WithDescription("activate tenant");
    }
}
