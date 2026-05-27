using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Notifications.Features.v1.MarkNotificationRead;

public static class MarkNotificationReadEndpoint
{
    internal static RouteHandlerBuilder MapMarkNotificationReadEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/{id:guid}/read",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("MarkNotificationRead")
            .WithSummary("Mark a single notification as read")
            .RequirePermission(NotificationPermissions.Inbox.MarkRead);
}
