using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Notifications.Features.v1.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    NotificationsDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<MarkNotificationReadCommand, Unit>
{
    public async ValueTask<Unit> Handle(MarkNotificationReadCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("no current user")
            {
                MessageKey = "Error.NoCurrentUser",
            };
        }
        var currentUserId = userId.ToString();

        // Caller-scoped: filter by (Id, UserId) so users can only mutate their own rows. Returns
        // 404 if the row exists but belongs to someone else — we don't leak existence.
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == cmd.NotificationId && n.UserId == currentUserId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Notification not found.")
            {
                MessageKey = "Notifications.NotificationNotFound",
                ResourceSource = typeof(NotificationsResources),
            };

        notification.MarkRead();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
