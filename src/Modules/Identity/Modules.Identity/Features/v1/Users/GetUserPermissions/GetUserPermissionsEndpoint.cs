using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserPermissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserPermissions;

public static class GetUserPermissionsEndpoint
{
    // No RequirePermission here on purpose: this endpoint returns the *caller's*
    // own permissions, which the SPA needs to render permission-gated routes.
    // Gating it behind Users.View locked out every role that doesn't manage
    // users (e.g. a Billing-only tenant operator), which would have meant
    // their client-side route guards never received any permissions and the
    // admin would stall on "Resolving permissions" forever. The fallback
    // policy still enforces RequireAuthenticatedUser, so anonymous callers
    // get 401 as expected.
    internal static RouteHandlerBuilder MapGetCurrentUserPermissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/permissions", async (ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            return TypedResults.Ok(await mediator.Send(new GetCurrentUserPermissionsQuery(userId), cancellationToken));
        })
        .WithName("GetCurrentUserPermissions")
        .WithSummary("Get current user permissions")
        .WithDescription("Retrieve permissions for the authenticated user. Requires authentication only — every signed-in user can read their own grants.")
        .Produces<IEnumerable<string>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}