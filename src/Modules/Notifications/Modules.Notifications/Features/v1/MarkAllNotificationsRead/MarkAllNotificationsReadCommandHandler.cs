using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Notifications.Features.v1.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler(
    NotificationsDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    public async ValueTask<int> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken cancellationToken)
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

        var now = DateTime.UtcNow;
        // Single bulk UPDATE — no row materialization, no domain events fired (none for this aggregate).
        var updated = await db.Notifications
            .Where(n => n.UserId == currentUserId && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, now), cancellationToken)
            .ConfigureAwait(false);

        return updated;
    }
}
