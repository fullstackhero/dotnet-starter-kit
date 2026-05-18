using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.GetImpersonationGrants;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Impersonation.GetImpersonationGrants;

public static class GetImpersonationGrantsEndpoint
{
    internal static RouteHandlerBuilder MapGetImpersonationGrantsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/impersonation/grants",
            async ([AsParameters] GetImpersonationGrantsQuery query,
                   IMediator mediator,
                   CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(query, ct)))
            .WithName("GetImpersonationGrants")
            .WithSummary("List impersonation grants")
            .WithDescription("Lists impersonation sessions scoped to what the caller can see. Tenant admins are limited to grants targeting their own tenant; root operators can filter by any tenant.")
            .RequirePermission(IdentityPermissions.Impersonation.View)
            .Produces<IReadOnlyList<ImpersonationGrantDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
