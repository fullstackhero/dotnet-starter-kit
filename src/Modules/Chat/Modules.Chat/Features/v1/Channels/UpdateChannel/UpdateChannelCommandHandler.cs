using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using FSH.Modules.Chat.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.UpdateChannel;

public sealed class UpdateChannelCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateChannelCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateChannelCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user") { MessageKey = "Error.NoCurrentUser" };

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.")
            {
                MessageKey = "Chat.ChannelNotFound",
                ResourceSource = typeof(ChatResources),
            };

        channel.RequireAdmin(userId.ToString());
        channel.Rename(cmd.Name, cmd.Description);
        channel.SetPrivate(cmd.IsPrivate);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
