using System.Collections.ObjectModel;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Contracts.v1.Queries;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.DiscoverChannels;

public sealed class DiscoverChannelsQueryHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : IQueryHandler<DiscoverChannelsQuery, ReadOnlyCollection<ChannelDto>>
{
    public async ValueTask<ReadOnlyCollection<ChannelDto>> Handle(DiscoverChannelsQuery q, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(q);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        int page = Math.Max(1, q.Page);
        int pageSize = Math.Clamp(q.PageSize, 1, 200);

        var query = db.Channels.AsNoTracking()
            .Where(c => c.Type == ChannelType.Channel
                     && !c.IsPrivate
                     && !c.Members.Any(m => m.UserId == currentUserId));

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.Name!, $"%{term}%")
                || EF.Functions.ILike(c.Slug!, $"%{term}%"));
        }

        var channels = await query
            .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return channels.Select(c => c.ToDto()).ToList().AsReadOnly();
    }
}
