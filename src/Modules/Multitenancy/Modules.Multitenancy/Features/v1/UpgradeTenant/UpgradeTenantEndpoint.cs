using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.UpgradeTenant;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.UpgradeTenant;

public static class UpgradeTenantEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/upgrade", Handler)
        .WithName("UpgradeTenant")
        .WithSummary("Upgrade tenant subscription")
        .RequirePermission(MultitenancyConstants.Permissions.Update)
        .WithDescription("Extend or upgrade a tenant's subscription.")
        .Produces<UpgradeTenantCommandResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<UpgradeTenantCommandResponse>, BadRequest>> Handler(
        string id,
        UpgradeTenantCommand command,
        IMediator dispatcher,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(id, command.Tenant, StringComparison.Ordinal))
        {
            return TypedResults.BadRequest();
        }

        var result = await dispatcher.Send(command, cancellationToken);
        return TypedResults.Ok(result);
    }
}
