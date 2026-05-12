using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Notifications.Features.v1.ListNotifications;

public static class ListNotificationsEndpoint
{
    internal static RouteHandlerBuilder MapListNotificationsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/",
                async (bool? unreadOnly, int? page, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(
                        new ListNotificationsQuery(unreadOnly ?? false, page ?? 1, pageSize ?? 50),
                        cancellationToken)))
            .WithName("ListNotifications")
            .WithSummary("List the caller's notifications (newest first)")
            .RequirePermission(NotificationPermissions.Inbox.View);
}
