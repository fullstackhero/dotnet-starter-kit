using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.MarkChannelRead;

public sealed class MarkChannelReadCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<MarkChannelReadCommand, Unit>
{
    public async ValueTask<Unit> Handle(MarkChannelReadCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");
        channel.RequireMember(currentUserId);

        // Verify the marker message actually exists in this channel.
        var exists = await db.Messages
            .AnyAsync(m => m.Id == cmd.MessageId && m.ChannelId == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists) throw new NotFoundException("Message not found in this channel.");

        channel.MarkRead(currentUserId, cmd.MessageId);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
