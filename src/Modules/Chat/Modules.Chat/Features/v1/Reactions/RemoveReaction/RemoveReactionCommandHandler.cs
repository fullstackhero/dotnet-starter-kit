using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using FSH.Modules.Chat.Localization;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Reactions.RemoveReaction;

public sealed class RemoveReactionCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IHubContext<AppHub> hub)
    : ICommandHandler<RemoveReactionCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveReactionCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user") { MessageKey = "Error.NoCurrentUser" };
        var currentUserId = userId.ToString();

        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == cmd.MessageId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.")
            {
                MessageKey = "Chat.MessageNotFound",
                ResourceSource = typeof(ChatResources),
            };

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == message.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.")
            {
                MessageKey = "Chat.MessageNotFound",
                ResourceSource = typeof(ChatResources),
            };
        channel.RequireMember(currentUserId);

        if (!message.RemoveReaction(currentUserId, cmd.Emoji))
        {
            // Already absent — idempotent no-op, no broadcast.
            return Unit.Value;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await hub.Clients.Group($"channel:{channel.Id}")
            .SendAsync("ChatReactionChanged",
                new { channelId = channel.Id, messageId = message.Id, userId = currentUserId, emoji = cmd.Emoji.Trim(), kind = "removed" },
                cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }
}
