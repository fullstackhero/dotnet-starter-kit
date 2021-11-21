using DN.WebApi.Application.Abstractions;
using DN.WebApi.Application.Abstractions.Services;
using DN.WebApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>, ITransientService
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public async override Task OnConnectedAsync()
        {
            string tenant = Context.User.GetTenant();
            if (string.IsNullOrEmpty(tenant))
            {
                throw new Exception();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"GroupTenant-{tenant}");
            await base.OnConnectedAsync();
            _logger.LogInformation("A client connected to NotificationHub: " + Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string tenant = Context.User.GetTenant();
            if (string.IsNullOrEmpty(tenant))
            {
                throw new Exception();
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"GroupTenant-{tenant}");
            await base.OnDisconnectedAsync(exception);
            _logger.LogInformation("A client disconnected from NotificationHub: " + Context.ConnectionId);
        }
    }
}
