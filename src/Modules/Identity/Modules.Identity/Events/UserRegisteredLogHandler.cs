using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user registration events for auditing purposes.
/// Note: This is a domain event handler. There's also UserRegisteredEmailHandler 
/// which handles the integration event for cross-module communication.
/// </summary>
public sealed class UserRegisteredLogHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredLogHandler> _logger;

    public UserRegisteredLogHandler(ILogger<UserRegisteredLogHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "User {UserId} ({Email}) registered in tenant {TenantId}",
                notification.UserId,
                notification.Email,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
