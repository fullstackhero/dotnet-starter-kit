using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.GetMyTenantStatus;

public static class GetMyTenantStatusEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/me/status", async (
                IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
                ITenantService tenantService,
                CancellationToken cancellationToken) =>
            {
                var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Results.Unauthorized();
                }

                var status = await tenantService.GetStatusAsync(tenantId, cancellationToken).ConfigureAwait(false);
                return Results.Ok(status);
            })
            .WithName("GetMyTenantStatus")
            .WithSummary("Get the calling tenant's status")
            .WithDescription("Returns plan, validity, and expiry/grace state for the authenticated tenant — used by the tenant dashboard to show plan info and expiry warnings.")
            .RequireAuthorization()
            .Produces<TenantStatusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
