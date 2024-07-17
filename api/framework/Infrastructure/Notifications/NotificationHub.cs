using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Notifications;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IMultiTenantContextAccessor _multiTenantContextAccessor;

    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IMultiTenantContextAccessor multiTenantContextAccessor, ILogger<NotificationHub> logger)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _logger = logger;
    }


    public override async Task OnConnectedAsync()
    {
        var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;
        if (currentTenant is null)
        {
            throw new UnauthorizedException("Authentication Failed.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"GroupTenant-{currentTenant.Id}");

        await base.OnConnectedAsync();

        _logger.LogInformation("A client connected to NotificationHub: {ConnectionId}", Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;
        if (currentTenant != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"GroupTenant-{currentTenant.Id}");
        }

        await base.OnDisconnectedAsync(exception);

        _logger.LogInformation("A client disconnected from NotificationHub: {ConnectionId}", Context.ConnectionId);
    }
}
