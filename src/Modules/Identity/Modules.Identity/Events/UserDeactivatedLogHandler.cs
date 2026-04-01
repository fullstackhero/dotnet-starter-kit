using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user deactivation events for auditing purposes.
/// </summary>
public sealed class UserDeactivatedLogHandler : INotificationHandler<UserDeactivatedEvent>
{
    private readonly ILogger<UserDeactivatedLogHandler> _logger;

    public UserDeactivatedLogHandler(ILogger<UserDeactivatedLogHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(UserDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "User {UserId} deactivated in tenant {TenantId}",
                notification.UserId,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
