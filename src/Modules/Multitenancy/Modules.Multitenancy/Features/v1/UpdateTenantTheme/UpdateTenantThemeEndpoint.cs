using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantTheme;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.UpdateTenantTheme;

public static class UpdateTenantThemeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/theme", async (TenantThemeDto theme, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new UpdateTenantThemeCommand(theme), cancellationToken);
                return Results.NoContent();
            })
            .WithName("UpdateTenantTheme")
            .WithSummary("Update current tenant theme")
            .WithDescription("Update the theme settings for the current tenant, including colors, typography, and layout.")
            .RequirePermission(MultitenancyConstants.Permissions.UpdateTheme)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
