using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.ToggleUserStatus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.ToggleUserStatus;

public static class ToggleUserStatusEndpoint
{
    internal static RouteHandlerBuilder MapToggleUserStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPatch("/users/{id:guid}", Handler)
        .WithName("ToggleUserStatus")
        .WithSummary("Toggle user status")
        .RequirePermission(IdentityPermissionConstants.Users.Update)
        .WithDescription("Activate or deactivate a user account.");
    }

    private static async Task<Results<NoContent, BadRequest>> Handler(
        string id,
        [FromBody] ToggleUserStatusCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            command.UserId = id;
        }

        if (!string.Equals(id, command.UserId, StringComparison.Ordinal))
        {
            return TypedResults.BadRequest();
        }

        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}
