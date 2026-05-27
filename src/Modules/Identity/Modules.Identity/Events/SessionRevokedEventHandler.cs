using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Handles the SessionRevokedEvent domain event.
/// </summary>
public sealed class SessionRevokedHandler(
    ILogger<SessionRevokedHandler> logger)
    : INotificationHandler<SessionRevokedEvent>
{
    public ValueTask Handle(SessionRevokedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Session {SessionId} revoked for user {UserId} by {RevokedBy}: {Reason}",
                notification.SessionId,
                notification.UserId,
                notification.RevokedBy,
                notification.Reason);
        }

        return ValueTask.CompletedTask;
    }
}
