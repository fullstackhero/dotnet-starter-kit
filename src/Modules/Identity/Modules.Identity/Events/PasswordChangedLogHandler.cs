using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs password change events for security auditing purposes.
/// </summary>
public sealed class PasswordChangedLogHandler : INotificationHandler<PasswordChangedEvent>
{
    private readonly ILogger<PasswordChangedLogHandler> _logger;

    public PasswordChangedLogHandler(ILogger<PasswordChangedLogHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Password {Action} for user {UserId} in tenant {TenantId}",
                notification.WasReset ? "reset" : "changed",
                notification.UserId,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
