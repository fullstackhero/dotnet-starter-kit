using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using FSH.Modules.Identity.Contracts.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Messages.DeleteMessage;

public sealed class DeleteMessageCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IUserPermissionService permissions)
    : ICommandHandler<DeleteMessageCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteMessageCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == cmd.MessageId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.");

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == message.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.");
        channel.RequireMember(currentUserId);

        bool isModerator = await permissions
            .HasPermissionAsync(currentUserId, ChatPermissions.Messages.DeleteAny, cancellationToken)
            .ConfigureAwait(false);

        message.SoftDelete(currentUserId, isModerator);

        // If this was a thread reply, decrement the parent's ReplyCount.
        if (message.ParentMessageId is { } parentId)
        {
            var parent = await db.Messages.FirstOrDefaultAsync(m => m.Id == parentId, cancellationToken)
                .ConfigureAwait(false);
            parent?.DecrementReplyCount();
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
