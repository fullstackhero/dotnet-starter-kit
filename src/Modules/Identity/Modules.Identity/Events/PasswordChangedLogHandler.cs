using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs password change events for security auditing purposes.
/// </summary>
public sealed class PasswordChangedLogHandler(ILogger<PasswordChangedLogHandler> logger) : INotificationHandler<PasswordChangedEvent>
{
    public ValueTask Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Password {Action} for user {UserId} in tenant {TenantId}",
                notification.WasReset ? "reset" : "changed",
                notification.UserId,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
