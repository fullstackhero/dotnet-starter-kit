using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Multitenancy.Contracts.Authorization;
using FSH.Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.AdjustTenantValidity;

public static class AdjustTenantValidityEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/adjust-validity", Handler)
            .WithName("AdjustTenantValidity")
            .WithSummary("Adjust tenant validity (operator override)")
            .RequirePermission(MultitenancyPermissions.Tenants.UpgradeSubscription)
            .WithDescription("Set a tenant's validity to an explicit date with no invoice or renewal event — for comps, support extensions, or immediate expiry.")
            .Produces<AdjustTenantValidityCommandResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<AdjustTenantValidityCommandResponse>, BadRequest>> Handler(
        string id,
        AdjustTenantValidityCommand command,
        IMediator dispatcher,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(id, command.TenantId, StringComparison.Ordinal))
        {
            return TypedResults.BadRequest();
        }

        var result = await dispatcher.Send(command, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }
}
