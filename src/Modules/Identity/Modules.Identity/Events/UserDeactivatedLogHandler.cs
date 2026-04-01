using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user deactivation events for auditing purposes.
/// </summary>
public sealed class UserDeactivatedLogHandler(ILogger<UserDeactivatedLogHandler> logger) : INotificationHandler<UserDeactivatedEvent>
{
    public ValueTask Handle(UserDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "User {UserId} deactivated in tenant {TenantId}",
                notification.UserId,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
