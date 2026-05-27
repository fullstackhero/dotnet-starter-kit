using System.Collections.ObjectModel;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Contracts.v1.Queries;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Messages.ListMessageReplies;

public sealed class ListMessageRepliesQueryHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : IQueryHandler<ListMessageRepliesQuery, ReadOnlyCollection<MessageDto>>
{
    public async ValueTask<ReadOnlyCollection<MessageDto>> Handle(
        ListMessageRepliesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        // Load the parent so we can authorize the caller through the channel.
        var parent = await db.Messages
            .Where(m => m.Id == query.ParentMessageId)
            .Select(m => new { m.Id, m.ChannelId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Parent message not found.");

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == parent.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Parent message not found.");
        channel.RequireMember(currentUserId);

        IQueryable<Domain.Message> q = db.Messages
            .Where(m => m.ParentMessageId == query.ParentMessageId);

        if (query.Before is { } beforeId)
        {
            q = q.Where(m => m.Id.CompareTo(beforeId) < 0);
        }

        var rows = await q
            .OrderByDescending(m => m.Id)
            .Take(query.PageSize)
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(m => m.ToDto()).ToList().AsReadOnly();
    }
}
