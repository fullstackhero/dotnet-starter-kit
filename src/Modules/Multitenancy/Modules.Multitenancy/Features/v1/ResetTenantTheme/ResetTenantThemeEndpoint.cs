using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.ResetTenantTheme;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.ResetTenantTheme;

public static class ResetTenantThemeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/theme/reset", async (IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new ResetTenantThemeCommand(), cancellationToken);
                return Results.NoContent();
            })
            .WithName("ResetTenantTheme")
            .WithSummary("Reset tenant theme to defaults")
            .WithDescription("Reset the theme settings for the current tenant to the default values.")
            .RequirePermission(MultitenancyConstants.Permissions.UpdateTheme)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
