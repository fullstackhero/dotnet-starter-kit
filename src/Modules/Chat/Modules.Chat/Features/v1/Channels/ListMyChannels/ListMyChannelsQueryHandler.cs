using System.Collections.ObjectModel;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Contracts.v1.Queries;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.ListMyChannels;

public sealed class ListMyChannelsQueryHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : IQueryHandler<ListMyChannelsQuery, ReadOnlyCollection<ChannelDto>>
{
    public async ValueTask<ReadOnlyCollection<ChannelDto>> Handle(ListMyChannelsQuery q, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(q);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        int page = Math.Max(1, q.Page);
        int pageSize = Math.Clamp(q.PageSize, 1, 200);

        var channels = await db.Channels.AsNoTracking()
            .Where(c => c.Members.Any(m => m.UserId == currentUserId))
            .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Single round-trip: count each channel's unread messages via a correlated subquery instead of
        // one CountAsync per channel (was N+1, up to 200 round-trips).
        var channelIds = channels.Select(c => c.Id).ToList();
        var unread = await db.Channels.AsNoTracking()
            .Where(c => channelIds.Contains(c.Id))
            .SelectMany(
                c => c.Members.Where(m => m.UserId == currentUserId),
                (c, m) => new
                {
                    c.Id,
                    Unread = db.Messages.Count(msg =>
                        msg.ChannelId == c.Id
                        && msg.DeletedAtUtc == null
                        && (m.LastReadMessageId == null || msg.Id.CompareTo(m.LastReadMessageId.Value) > 0)),
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var unreadMap = unread.ToDictionary(x => x.Id, x => x.Unread);

        return channels.Select(c => c.ToDto(unreadMap.GetValueOrDefault(c.Id))).ToList().AsReadOnly();
    }
}
