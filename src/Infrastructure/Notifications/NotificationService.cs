using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Shared.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace DN.WebApi.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ITenantService _tenantService;

    public NotificationService(IHubContext<NotificationHub> notificationHubContext, ITenantService tenantService) =>
        (_notificationHubContext, _tenantService) = (notificationHubContext, tenantService);

    #region RootTenantMethods

    public Task BroadcastMessageAsync(INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.All
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.AllExcept(excludedConnectionIds)
            .SendAsync(notification.MessageType, notification, cancellationToken);

    #endregion RootTenantMethods

    public Task SendMessageAsync(INotificationMessage notification, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.Group($"GroupTenant-{CurrentTenantKey}")
            .SendAsync(notification.MessageType, notification, cancellationToken);

    public Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken) =>
        _notificationHubContext.Clients.GroupExcept($"GroupTenant-{CurrentTenantKey}", excludedConnectionIds)
            .SendAsync(notification.MessageType, notification, cancellationToken);
    private string? CurrentTenantKey => _tenantService.GetCurrentTenant()?.Key;

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