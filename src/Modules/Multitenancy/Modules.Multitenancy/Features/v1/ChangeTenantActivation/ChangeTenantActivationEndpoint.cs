using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.ChangeTenantActivation;

public static class ChangeTenantActivationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/activation", Handler)
            .WithName("ChangeTenantActivation")
            .WithSummary("Change tenant activation state")
            .WithDescription("Activate or deactivate a tenant in a single endpoint.")
            .RequirePermission(MultitenancyConstants.Permissions.Update)
            .Produces<TenantLifecycleResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<TenantLifecycleResultDto>, BadRequest>> Handler(
        [FromRoute] string id,
        [FromBody] ChangeTenantActivationCommand command,
        IMediator mediator)
    {
        if (!string.Equals(id, command.TenantId, StringComparison.Ordinal))
        {
            return TypedResults.BadRequest();
        }

        TenantLifecycleResultDto result = await mediator.Send(command);
        return TypedResults.Ok(result);
    }
}
