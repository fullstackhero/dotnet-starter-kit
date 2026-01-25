using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantMigrations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenantMigrations;

public static class TenantMigrationsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
                "/migrations",
                async (IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyCollection<TenantMigrationStatusDto> result =
                        await mediator.Send(new GetTenantMigrationsQuery(), cancellationToken);

                    return TypedResults.Ok(result);
                })
            .WithName("GetTenantMigrations")
            .RequirePermission(MultitenancyConstants.Permissions.View)
            .WithSummary("Get per-tenant migration status")
            .WithDescription("Retrieve migration status for each tenant, including pending migrations and provider information.")
            .Produces<IReadOnlyCollection<TenantMigrationStatusDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}

