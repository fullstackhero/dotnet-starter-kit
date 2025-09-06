using FluentValidation;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Identity.Users.Features.ToggleUserStatus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;

public static class ToggleUserStatusEndpoint
{
    internal static RouteHandlerBuilder ToggleUserStatusEndpointEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id:guid}/toggle-status", async (
            string id,
            ToggleUserStatusCommand command,
            [FromServices] IUserService userService,
            CancellationToken cancellationToken) =>
        {
            if (id != command.UserId)
            {
                return Results.BadRequest();
            }

            await userService.ToggleStatusAsync(command, cancellationToken);
            return Results.Ok();
        })
        .WithName(nameof(ToggleUserStatusEndpoint))
        .WithSummary("Toggle a user's active status")
        .WithDescription("Toggle a user's active status")
        .AllowAnonymous();
    }

}
