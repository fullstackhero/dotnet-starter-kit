using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using Mediator;

namespace FSH.Modules.Chat.Features.v1.Channels.CreateChannel;

public sealed class CreateChannelCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<CreateChannelCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateChannelCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId().ToString();
        if (userId == Guid.Empty.ToString())
        {
            throw new UnauthorizedException("no current user");
        }

        var channel = ChatChannel.CreateChannel(cmd.Name, cmd.Description, cmd.IsPrivate, userId);
        db.Channels.Add(channel);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return channel.Id;
    }
}
