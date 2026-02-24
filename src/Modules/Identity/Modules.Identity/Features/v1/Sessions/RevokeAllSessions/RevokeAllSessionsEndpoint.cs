using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Sessions.RevokeAllSessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Sessions.RevokeAllSessions;

public static class RevokeAllSessionsEndpoint
{
    internal static RouteHandlerBuilder MapRevokeAllSessionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/sessions/revoke-all", async (RevokeAllSessionsCommand? command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command ?? new RevokeAllSessionsCommand(), cancellationToken);
            return TypedResults.Ok(new { RevokedCount = result });
        })
        .WithName("RevokeAllSessions")
        .WithSummary("Revoke all sessions")
        .RequirePermission(IdentityPermissionConstants.Sessions.Revoke)
        .WithDescription("Revoke all sessions for the currently authenticated user except the current one.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
