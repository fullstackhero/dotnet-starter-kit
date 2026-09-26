using System.Collections.ObjectModel;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Contracts.v1.Queries;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Features.v1.Internal;
using FSH.Modules.Chat.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Messages.GetPinnedMessages;

public sealed class GetPinnedMessagesQueryHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IQueryHandler<GetPinnedMessagesQuery, ReadOnlyCollection<MessageDto>>
{
    public async ValueTask<ReadOnlyCollection<MessageDto>> Handle(
        GetPinnedMessagesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user") { MessageKey = "Error.NoCurrentUser" };
        var currentUserId = userId.ToString();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == query.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.")
            {
                MessageKey = "Chat.ChannelNotFound",
                ResourceSource = typeof(ChatResources),
            };
        channel.RequireMember(currentUserId);

        var rows = await db.Messages
            .Where(m => m.ChannelId == query.ChannelId && m.IsPinned)
            .OrderByDescending(m => m.PinnedAtUtc)
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = rows.Select(m => m.ToDto()).ToList();
        var resolved = await ChatAttachmentUrls.ResolveAsync(dtos, mediator, cancellationToken).ConfigureAwait(false);
        return resolved.AsReadOnly();
    }
}
