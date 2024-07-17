using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Notifications;
using Microsoft.AspNetCore.SignalR;
using static FSH.Framework.Core.Notifications.NotificationConstants;

namespace FSH.Framework.Infrastructure.Notifications;

public class NotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly IMultiTenantContextAccessor _multiTenantContextAccessor;

    public NotificationSender(IHubContext<NotificationHub> notificationHubContext, IMultiTenantContextAccessor multiTenantContextAccessor) =>
        (_notificationHubContext, _multiTenantContextAccessor) = (notificationHubContext, multiTenantContextAccessor);

    public Task BroadcastAsync(INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.All
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);

    public Task BroadcastAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.AllExcept(excludedConnectionIds)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);

    public Task SendToAllAsync(INotificationMessage notification, CancellationToken cancellationToken)
    {
        var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;
        if (currentTenant == null)
        {
            throw new UnauthorizedException("Authentication Failed.");
        }

        return _notificationHubContext.Clients.Group($"GroupTenant-{currentTenant.Id}")
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);
    }

    public Task SendToAllAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken)
    {
        var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;
        if (currentTenant == null)
        {
            throw new UnauthorizedException("Authentication Failed.");
        }

        return _notificationHubContext.Clients.GroupExcept($"GroupTenant-{currentTenant.Id}", excludedConnectionIds)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);
    }

    public Task SendToGroupAsync(INotificationMessage notification, string group, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Group(group)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);

    public Task SendToGroupAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.GroupExcept(group, excludedConnectionIds)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);

    public Task SendToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Groups(groupNames)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);

    public Task SendToUserAsync(INotificationMessage notification, string userId, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.User(userId)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);

    public Task SendToUsersAsync(INotificationMessage notification, IEnumerable<string> userIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Users(userIds)
            .SendAsync(NotificationFromServer, notification.GetType().FullName, notification, cancellationToken);
}
