using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs session revocation events for security auditing purposes.
/// </summary>
public sealed class SessionRevokedLogHandler : INotificationHandler<SessionRevokedEvent>
{
    private readonly ILogger<SessionRevokedLogHandler> _logger;

    public SessionRevokedLogHandler(ILogger<SessionRevokedLogHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(SessionRevokedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Session {SessionId} revoked for user {UserId} in tenant {TenantId}. Reason: {Reason}",
                notification.SessionId,
                notification.UserId,
                notification.TenantId ?? "root",
                notification.RevokedBy);
        }

        return ValueTask.CompletedTask;
    }
}
