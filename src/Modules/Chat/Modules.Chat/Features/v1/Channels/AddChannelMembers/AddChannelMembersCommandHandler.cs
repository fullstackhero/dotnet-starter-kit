using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.AddChannelMembers;

public sealed class AddChannelMembersCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IHubContext<AppHub> hub)
    : ICommandHandler<AddChannelMembersCommand, Unit>
{
    public async ValueTask<Unit> Handle(AddChannelMembersCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");

        // Members can invite to public channels they belong to; private channels require Admin.
        var caller = channel.RequireMember(currentUserId);
        if (channel.IsPrivate && caller.Role != ChannelMemberRole.Admin)
        {
            throw new ForbiddenException("Only channel admins can add members to private channels.");
        }

        var newlyAdded = new List<string>();
        foreach (var uid in cmd.UserIds.Distinct(StringComparer.Ordinal))
        {
            // Skip duplicates silently — endpoint is idempotent for already-members.
            if (channel.Members.Any(m => string.Equals(m.UserId, uid, StringComparison.Ordinal))) continue;
            channel.AddMember(uid, currentUserId);
            newlyAdded.Add(uid);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var uid in newlyAdded)
        {
            // Notify existing members. The new member doesn't have any active connections in
            // the channel:{id} group yet — they'll pick the channel up on their next reconnect
            // (OnConnectedAsync pre-joins all of their channels).
            await hub.Clients.Group($"channel:{channel.Id}")
                .SendAsync("ChatChannelMemberAdded", new { channelId = channel.Id, userId = uid }, cancellationToken)
                .ConfigureAwait(false);
            await hub.Clients.Group($"user:{uid}")
                .SendAsync("ChatChannelAdded", new { channelId = channel.Id }, cancellationToken)
                .ConfigureAwait(false);
        }
        return Unit.Value;
    }
}
