using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Notifications.Features.v1.MarkAllNotificationsRead;

public static class MarkAllNotificationsReadEndpoint
{
    internal static RouteHandlerBuilder MapMarkAllNotificationsReadEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/read-all",
                async (IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(new { updated = await mediator.Send(new MarkAllNotificationsReadCommand(), cancellationToken) }))
            .WithName("MarkAllNotificationsRead")
            .WithSummary("Mark every unread notification for the caller as read; returns the count updated")
            .RequirePermission(NotificationPermissions.Inbox.MarkRead);
}
