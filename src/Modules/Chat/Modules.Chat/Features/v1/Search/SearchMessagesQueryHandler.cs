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

namespace FSH.Modules.Chat.Features.v1.Search;

public sealed class SearchMessagesQueryHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IQueryHandler<SearchMessagesQuery, ReadOnlyCollection<MessageDto>>
{
    public async ValueTask<ReadOnlyCollection<MessageDto>> Handle(
        SearchMessagesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user") { MessageKey = "Error.NoCurrentUser" };
        var currentUserId = userId.ToString();

        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Leakage guard: scope the search to channels the caller is a member of (intersected with the
        // requested channel if any) so cross-channel results never reach non-members.
        IQueryable<Guid> memberChannelIds = db.Channels.AsNoTracking()
            .Where(c => c.Members.Any(m => m.UserId == currentUserId))
            .Select(c => c.Id);

        if (query.ChannelId is { } scopedChannelId)
        {
            memberChannelIds = memberChannelIds.Where(id => id == scopedChannelId);
        }

        var allowedChannelIds = await memberChannelIds.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (allowedChannelIds.Count == 0)
        {
            return new List<MessageDto>().AsReadOnly();
        }

        // Interpolation is parameterized (sanitized literal, not raw SQL); websearch_to_tsquery lets
        // callers use natural syntax (quoted phrases, OR, -exclude) with no pre-processing.
        int offset = (page - 1) * pageSize;
        FormattableString sql = $@"
SELECT m.*
FROM chat.""Messages"" m
WHERE m.""ChannelId"" = ANY({allowedChannelIds.ToArray()})
  AND m.""DeletedAtUtc"" IS NULL
  AND m.""BodyTsv"" @@ websearch_to_tsquery('english', {query.Q})
ORDER BY ts_rank(m.""BodyTsv"", websearch_to_tsquery('english', {query.Q})) DESC,
         m.""Id"" DESC
LIMIT {pageSize} OFFSET {offset}
";

        var rows = await db.Messages
            .FromSqlInterpolated(sql)
            .AsNoTracking()
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = rows.Select(m => m.ToDto()).ToList();
        var resolved = await ChatAttachmentUrls.ResolveAsync(dtos, mediator, cancellationToken).ConfigureAwait(false);
        return resolved.AsReadOnly();
    }
}
