using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.ArchiveChannel;

public sealed class ArchiveChannelCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<ArchiveChannelCommand, Unit>
{
    public async ValueTask<Unit> Handle(ArchiveChannelCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");

        channel.RequireAdmin(userId.ToString());

        // Remove triggers ISoftDeletable via the framework's audit interceptor.
        db.Channels.Remove(channel);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
