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

        // Batch the unread counts in a single query to avoid N+1.
        var channelIds = channels.Select(c => c.Id).ToList();
        var unreadByChannel = await db.Channels.AsNoTracking()
            .Where(c => channelIds.Contains(c.Id))
            .SelectMany(c => c.Members.Where(m => m.UserId == currentUserId), (c, m) => new { c.Id, LastRead = m.LastReadMessageId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var unreadMap = new Dictionary<Guid, int>();
        foreach (var row in unreadByChannel)
        {
            unreadMap[row.Id] = await db.Messages.AsNoTracking()
                .Where(msg => msg.ChannelId == row.Id
                    && msg.DeletedAtUtc == null
                    && (row.LastRead == null || msg.Id.CompareTo(row.LastRead.Value) > 0))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return channels.Select(c => c.ToDto(unreadMap.GetValueOrDefault(c.Id))).ToList().AsReadOnly();
    }
}
