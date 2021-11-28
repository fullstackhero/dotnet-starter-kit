using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Infrastructure.Hubs;
using DN.WebApi.Shared.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace DN.WebApi.Infrastructure.Common.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ITenantService _tenantService;

    public NotificationService(IHubContext<NotificationHub> notificationHubContext, ITenantService tenantService)
    {
        _notificationHubContext = notificationHubContext;
        _tenantService = tenantService;
    }

    #region RootTenantMethods

    public async Task BroadcastMessageAsync(INotificationMessage notification)
    {
        await _notificationHubContext.Clients.All.SendAsync(notification.MessageType, notification);
    }

    public async Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds)
    {
        await _notificationHubContext.Clients.AllExcept(excludedConnectionIds).SendAsync(notification.MessageType, notification);
    }

    #endregion RootTenantMethods

    public async Task SendMessageAsync(INotificationMessage notification)
    {
        var tenant = _tenantService.GetCurrentTenant();
        await _notificationHubContext.Clients.Group($"GroupTenant-{tenant.Key}").SendAsync(notification.MessageType, notification);
    }

    public async Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds)
    {
        var tenant = _tenantService.GetCurrentTenant();
        await _notificationHubContext.Clients.GroupExcept($"GroupTenant-{tenant.Key}", excludedConnectionIds).SendAsync(notification.MessageType, notification);
    }

    public async Task SendMessageToGroupAsync(INotificationMessage notification, string group)
    {
        await _notificationHubContext.Clients.Group(group).SendAsync(notification.MessageType, notification);
    }

    public async Task SendMessageToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames)
    {
        await _notificationHubContext.Clients.Groups(groupNames).SendAsync(notification.MessageType, notification);
    }

    public async Task SendMessageToGroupExceptAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds)
    {
        await _notificationHubContext.Clients.GroupExcept(group, excludedConnectionIds).SendAsync(notification.MessageType, notification);
    }

    public async Task SendMessageToUserAsync(string userId, INotificationMessage notification)
    {
        await _notificationHubContext.Clients.User(userId).SendAsync(notification.MessageType, notification);
    }

    public async Task SendMessageToUsersAsync(IEnumerable<string> userIds, INotificationMessage notification)
    {
        await _notificationHubContext.Clients.Users(userIds).SendAsync(notification.MessageType, notification);
    }
}