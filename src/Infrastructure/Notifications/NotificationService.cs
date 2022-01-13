using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Shared.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace FSH.WebApi.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ITenantInfo _currentTenant;

    public NotificationService(IHubContext<NotificationHub> notificationHubContext, ITenantInfo currentTenant) =>
        (_notificationHubContext, _currentTenant) = (notificationHubContext, currentTenant);

    #region RootTenantMethods

    public Task BroadcastMessageAsync(INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.All
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.AllExcept(excludedConnectionIds)
            .SendAsync(notification.MessageType, notification, cancellationToken);

    #endregion RootTenantMethods

    public Task SendMessageAsync(INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Group($"GroupTenant-{_currentTenant.Id}")
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.GroupExcept($"GroupTenant-{_currentTenant.Id}", excludedConnectionIds)
            .SendAsync(notification.MessageType, notification, cancellationToken);
    public Task SendMessageToGroupAsync(INotificationMessage notification, string group, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Group(group)
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task SendMessageToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Groups(groupNames)
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task SendMessageToGroupExceptAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.GroupExcept(group, excludedConnectionIds)
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task SendMessageToUserAsync(string userId, INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.User(userId)
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task SendMessageToUsersAsync(IEnumerable<string> userIds, INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Users(userIds)
            .SendAsync(notification.MessageType, notification, cancellationToken);
}