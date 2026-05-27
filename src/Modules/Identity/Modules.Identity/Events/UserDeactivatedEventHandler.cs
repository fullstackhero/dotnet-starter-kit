using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Handles the UserDeactivatedEvent domain event.
/// </summary>
public sealed class UserDeactivatedHandler(
    ILogger<UserDeactivatedHandler> logger)
    : INotificationHandler<UserDeactivatedEvent>
{
    public ValueTask Handle(UserDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "User {UserId} deactivated by {DeactivatedBy}: {Reason}",
                notification.UserId,
                notification.DeactivatedBy,
                notification.Reason);
        }

        return ValueTask.CompletedTask;
    }
}
