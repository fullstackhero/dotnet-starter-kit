using System.Collections.Generic;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Infrastructure.Hubs;
using DN.WebApi.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _notificationHubContext;
        private readonly ITenantService _tenantService;

        public NotificationService(IHubContext<NotificationHub, INotificationClient> notificationHubContext, ITenantService tenantService)
        {
            _notificationHubContext = notificationHubContext;
            _tenantService = tenantService;
        }

        #region RootTenantMethods
        public async Task BroadcastMessageAsync(INotificationMessage notification)
        {
            await _notificationHubContext.Clients.All.ReceiveMessage(notification);
        }

        public async Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds)
        {
            await _notificationHubContext.Clients.AllExcept(excludedConnectionIds).ReceiveMessage(notification);
        }
        #endregion

        public async Task SendMessageAsync(INotificationMessage notification)
        {
            var tenant = _tenantService.GetCurrentTenant();
            await _notificationHubContext.Clients.Group($"GroupTenant-{tenant.Key}").ReceiveMessage(notification);
        }

        public async Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds)
        {
            var tenant = _tenantService.GetCurrentTenant();
            await _notificationHubContext.Clients.GroupExcept($"GroupTenant-{tenant.Key}", excludedConnectionIds).ReceiveMessage(notification);
        }

        public async Task SendMessageToGroupAsync(INotificationMessage notification, string group)
        {
            await _notificationHubContext.Clients.Group(group).ReceiveMessage(notification);
        }

        public async Task SendMessageToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames)
        {
            await _notificationHubContext.Clients.Groups(groupNames).ReceiveMessage(notification);
        }

        public async Task SendMessageToGroupExceptAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds)
        {
            await _notificationHubContext.Clients.GroupExcept(group, excludedConnectionIds).ReceiveMessage(notification);
        }

        public async Task SendMessageToUserAsync(string userId, INotificationMessage notification)
        {
            await _notificationHubContext.Clients.User(userId).ReceiveMessage(notification);
        }

        public async Task SendMessageToUsersAsync(IEnumerable<string> userIds, INotificationMessage notification)
        {
            await _notificationHubContext.Clients.Users(userIds).ReceiveMessage(notification);
        }

    }
}