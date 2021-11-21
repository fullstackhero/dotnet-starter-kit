using DN.WebApi.Application.Abstractions;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Infrastructure.Hubs;
using DN.WebApi.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub, INotificationClient> _notificationHubContext;
        private readonly ITenantService _tenantService;
        private readonly ISerializerService _serializerService;

        public NotificationService(ILogger<NotificationService> logger, IHubContext<NotificationHub, INotificationClient> notificationHubContext, ITenantService tenantService, ISerializerService serializerService)
        {
            _logger = logger;
            _notificationHubContext = notificationHubContext;
            _tenantService = tenantService;
            _serializerService = serializerService;
        }

        #region RootTenantMethods
        public async Task BroadcastMessageAsync(INotificationMessage notification)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.All.ReceiveMessage(message);
        }

        public async Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.AllExcept(excludedConnectionIds).ReceiveMessage(message);
        }
        #endregion

        public async Task SendMessageAsync(INotificationMessage notification)
        {
            var tenant = _tenantService.GetCurrentTenant();
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.Group($"GroupTenant-{tenant.Key}").ReceiveMessage(message);
        }

        public async Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds)
        {
            var tenant = _tenantService.GetCurrentTenant();
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.GroupExcept($"GroupTenant-{tenant.Key}", excludedConnectionIds).ReceiveMessage(message);
        }

        public async Task SendMessageToGroupAsync(INotificationMessage notification, string group)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.Group(group).ReceiveMessage(message);
        }

        public async Task SendMessageToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.Groups(groupNames).ReceiveMessage(message);
        }

        public async Task SendMessageToGroupExceptAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.GroupExcept(group, excludedConnectionIds).ReceiveMessage(message);
        }

        public async Task SendMessageToUserAsync(string userId, INotificationMessage notification)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.User(userId).ReceiveMessage(message);
        }

        public async Task SendMessageToUsersAsync(IEnumerable<string> userIds, INotificationMessage notification)
        {
            string message = _serializerService.Serialize(notification);
            await _notificationHubContext.Clients.Users(userIds).ReceiveMessage(message);
        }

    }
}