using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs session revocation events for security auditing purposes.
/// </summary>
public sealed class SessionRevokedLogHandler(ILogger<SessionRevokedLogHandler> logger) : INotificationHandler<SessionRevokedEvent>
{
    public ValueTask Handle(SessionRevokedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Session {SessionId} revoked for user {UserId} in tenant {TenantId}. Reason: {Reason}",
                notification.SessionId,
                notification.UserId,
                notification.TenantId ?? "root",
                notification.RevokedBy);
        }

        return ValueTask.CompletedTask;
    }
}
