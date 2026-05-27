using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.RemoveChannelMember;

public sealed class RemoveChannelMemberCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IHubContext<AppHub> hub)
    : ICommandHandler<RemoveChannelMemberCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveChannelMemberCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");

        // Self-leave is always allowed for the current user. Removing someone else requires Admin.
        var isSelfLeave = string.Equals(cmd.UserId, currentUserId, StringComparison.Ordinal);
        if (!isSelfLeave)
        {
            channel.RequireAdmin(currentUserId);
        }
        else
        {
            channel.RequireMember(currentUserId);
        }

        channel.RemoveMember(cmd.UserId, currentUserId);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await hub.Clients.Group($"channel:{channel.Id}")
            .SendAsync("ChatChannelMemberRemoved", new { channelId = channel.Id, userId = cmd.UserId }, cancellationToken)
            .ConfigureAwait(false);
        await hub.Clients.Group($"user:{cmd.UserId}")
            .SendAsync("ChatChannelRemoved", new { channelId = channel.Id }, cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }
}
