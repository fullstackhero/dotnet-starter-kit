using DN.WebApi.Application.Abstractions;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Infrastructure.Hubs;
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

        public NotificationService(ILogger<NotificationService> logger, IHubContext<NotificationHub, INotificationClient> notificationHubContext, ITenantService tenantService)
        {
            _logger = logger;
            _notificationHubContext = notificationHubContext;
            _tenantService = tenantService;
        }

        #region RootTenantMethods
        public async Task BroadcastMessageAsync(object message)
        {
            await _notificationHubContext.Clients.All.ReceiveMessage(message);
        }

        public async Task BroadcastExceptMessageAsync(object message, IEnumerable<string> excludedConnectionIds)
        {
            await _notificationHubContext.Clients.AllExcept(excludedConnectionIds).ReceiveMessage(message);
        }
        #endregion

        public async Task SendMessageAsync(object message)
        {
            var tenant = _tenantService.GetCurrentTenant();
            await _notificationHubContext.Clients.Group($"Group-{tenant.Key}").ReceiveMessage(message);
        }

        public async Task SendMessageExceptAsync(object message, IEnumerable<string> excludedConnectionIds)
        {
            var tenant = _tenantService.GetCurrentTenant();
            await _notificationHubContext.Clients.GroupExcept($"Group-{tenant.Key}", excludedConnectionIds).ReceiveMessage(message);
        }

        public async Task SendMessageToGroupAsync(object message, string group)
        {
            await _notificationHubContext.Clients.Group(group).ReceiveMessage(message);
        }

        public async Task SendMessageToGroupsAsync(object message, IEnumerable<string> groupNames)
        {
            await _notificationHubContext.Clients.Groups(groupNames).ReceiveMessage(message);
        }

        public async Task SendMessageToGroupExceptAsync(object message, string group, IEnumerable<string> excludedConnectionIds)
        {
            await _notificationHubContext.Clients.GroupExcept(group, excludedConnectionIds).ReceiveMessage(message);
        }

        public async Task SendMessageToUserAsync(string user, object message)
        {
            await _notificationHubContext.Clients.User(user).ReceiveMessage(message);
        }

        public async Task SendMessageToUsersAsync(IEnumerable<string> userIds, object message)
        {
            await _notificationHubContext.Clients.Users(userIds).ReceiveMessage(message);
        }

    }
}