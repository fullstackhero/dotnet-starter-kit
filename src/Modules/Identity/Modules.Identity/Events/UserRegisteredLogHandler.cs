using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user registration events for auditing purposes.
/// Note: This is a domain event handler. There's also UserRegisteredEmailHandler 
/// which handles the integration event for cross-module communication.
/// </summary>
public sealed class UserRegisteredLogHandler(ILogger<UserRegisteredLogHandler> logger) : INotificationHandler<UserRegisteredEvent>
{
    public ValueTask Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "User {UserId} ({Email}) registered in tenant {TenantId}",
                notification.UserId,
                notification.Email,
                notification.TenantId ?? "root");
        }

        return ValueTask.CompletedTask;
    }
}
