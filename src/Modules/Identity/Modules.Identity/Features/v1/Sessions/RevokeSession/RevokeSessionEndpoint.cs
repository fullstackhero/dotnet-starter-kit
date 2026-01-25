using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Sessions.RevokeSession;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Sessions.RevokeSession;

public static class RevokeSessionEndpoint
{
    internal static RouteHandlerBuilder MapRevokeSessionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/sessions/{sessionId:guid}", Handler)
        .WithName("RevokeSession")
        .WithSummary("Revoke a session")
        .RequirePermission(IdentityPermissionConstants.Sessions.Revoke)
        .WithDescription("Revoke a specific session for the currently authenticated user.");
    }

    private static async Task<Results<Ok, NotFound>> Handler(
        Guid sessionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RevokeSessionCommand(sessionId), cancellationToken);
        return result ? TypedResults.Ok() : TypedResults.NotFound();
    }
}
