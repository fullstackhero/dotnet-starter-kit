using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Handles the PasswordChangedEvent domain event.
/// </summary>
public sealed class PasswordChangedHandler(
    ILogger<PasswordChangedHandler> logger)
    : INotificationHandler<PasswordChangedEvent>
{
    public ValueTask Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Password changed for user {UserId} (reset: {WasReset})",
                notification.UserId,
                notification.WasReset);
        }

        return ValueTask.CompletedTask;
    }
}
