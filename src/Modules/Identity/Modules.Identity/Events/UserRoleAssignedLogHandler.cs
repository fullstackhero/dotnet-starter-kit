using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Logs user role assignment events for auditing purposes.
/// </summary>
public sealed class UserRoleAssignedLogHandler(ILogger<UserRoleAssignedLogHandler> logger) : INotificationHandler<UserRoleAssignedEvent>
{
    public ValueTask Handle(UserRoleAssignedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            var roles = string.Join(", ", notification.AssignedRoles);
            logger.LogInformation(
                "Roles assigned to user {UserId} in tenant {TenantId}: {Roles}",
                notification.UserId,
                notification.TenantId ?? "root",
                roles);
        }

        return ValueTask.CompletedTask;
    }
}
