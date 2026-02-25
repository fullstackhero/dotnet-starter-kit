using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.CreateTenant;

public static class CreateTenantEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (
            [FromBody] CreateTenantCommand command,
            [FromServices] IMediator mediator)
            =>
            {
                var result = await mediator.Send(command);
                return TypedResults.Created($"/api/v1/multitenancy/tenants/{result.Id}", result);
            })
            .WithName("CreateTenant")
            .WithSummary("Create tenant")
            .RequirePermission(MultitenancyConstants.Permissions.Create)
            .WithDescription("Create a new tenant.")
            .Produces<CreateTenantCommandResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
