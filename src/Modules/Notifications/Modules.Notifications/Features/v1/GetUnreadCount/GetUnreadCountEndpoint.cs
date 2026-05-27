using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Notifications.Features.v1.GetUnreadCount;

public static class GetUnreadCountEndpoint
{
    internal static RouteHandlerBuilder MapGetUnreadCountEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/unread-count",
                async (IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new GetUnreadCountQuery(), cancellationToken)))
            .WithName("GetUnreadNotificationCount")
            .WithSummary("Count of caller's unread notifications (bell badge)")
            .RequirePermission(NotificationPermissions.Inbox.View);
}
