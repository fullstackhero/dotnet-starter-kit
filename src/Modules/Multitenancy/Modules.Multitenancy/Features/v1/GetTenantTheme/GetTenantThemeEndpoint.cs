using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantTheme;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenantTheme;

public static class GetTenantThemeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/theme", async (IMediator mediator, CancellationToken cancellationToken) =>
                await mediator.Send(new GetTenantThemeQuery(), cancellationToken))
            .WithName("GetTenantTheme")
            .WithSummary("Get current tenant theme")
            .WithDescription("Retrieve the theme settings for the current tenant, including colors, typography, and brand assets.")
            .RequirePermission(MultitenancyConstants.Permissions.ViewTheme)
            .Produces<TenantThemeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
