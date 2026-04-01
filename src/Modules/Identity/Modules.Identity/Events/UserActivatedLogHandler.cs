using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user activation events for auditing purposes.
/// </summary>
public sealed class UserActivatedLogHandler : INotificationHandler<UserActivatedEvent>
{
    private readonly ILogger<UserActivatedLogHandler> _logger;

    public UserActivatedLogHandler(ILogger<UserActivatedLogHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(UserActivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "User {UserId} activated in tenant {TenantId}",
                notification.UserId,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
