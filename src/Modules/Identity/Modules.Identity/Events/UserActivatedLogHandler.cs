using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user activation events for auditing purposes.
/// </summary>
public sealed class UserActivatedLogHandler(ILogger<UserActivatedLogHandler> logger) : INotificationHandler<UserActivatedEvent>
{
    public ValueTask Handle(UserActivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "User {UserId} activated in tenant {TenantId}",
                notification.UserId,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
