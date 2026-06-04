using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.RevokeImpersonationGrant;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;

public static class RevokeImpersonationGrantEndpoint
{
    public sealed record Body(string? Reason);

    internal static RouteHandlerBuilder MapRevokeImpersonationGrantEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/impersonation/grants/{id:guid}/revoke",
            async (Guid id,
                   [FromBody] Body? body,
                   IMediator mediator,
                   CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(
                    new RevokeImpersonationGrantCommand(id, body?.Reason), ct)))
            .WithName("RevokeImpersonationGrant")
            .WithSummary("Revoke an impersonation grant")
            .WithDescription("Marks the grant as revoked. Subsequent requests carrying the impersonation token are rejected by the JWT validation hook within ~1 second (cache TTL).")
            .RequirePermission(IdentityPermissions.Impersonation.Revoke)
            .Produces<ImpersonationGrantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}
