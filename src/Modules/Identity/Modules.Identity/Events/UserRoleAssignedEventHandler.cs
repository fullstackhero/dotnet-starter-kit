using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Handles the UserRoleAssignedEvent domain event.
/// </summary>
public sealed class UserRoleAssignedHandler(
    ILogger<UserRoleAssignedHandler> logger)
    : INotificationHandler<UserRoleAssignedEvent>
{
    public ValueTask Handle(UserRoleAssignedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Roles assigned to user {UserId}: {Roles}",
                notification.UserId,
                string.Join(", ", notification.AssignedRoles));
        }

        return ValueTask.CompletedTask;
    }
}
