using DN.WebApi.Application.Abstractions;
using DN.WebApi.Application.Abstractions.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Hubs
{
    public class NotificationHub : Hub<INotificationClient>, ITransientService
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }
    }
}
