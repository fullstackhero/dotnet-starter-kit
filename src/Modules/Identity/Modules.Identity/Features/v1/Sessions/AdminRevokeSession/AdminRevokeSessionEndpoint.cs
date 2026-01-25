using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeSession;

public static class AdminRevokeSessionEndpoint
{
    internal static RouteHandlerBuilder MapAdminRevokeSessionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/users/{userId:guid}/sessions/{sessionId:guid}", Handler)
        .WithName("AdminRevokeSession")
        .WithSummary("Revoke a user's session (Admin)")
        .RequirePermission(IdentityPermissionConstants.Sessions.RevokeAll)
        .WithDescription("Revoke a specific session for a user. Requires admin permission.");
    }

    private static async Task<Results<Ok, NotFound>> Handler(
        Guid userId,
        Guid sessionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AdminRevokeSessionCommand(userId, sessionId), cancellationToken);
        return result ? TypedResults.Ok() : TypedResults.NotFound();
    }
}
