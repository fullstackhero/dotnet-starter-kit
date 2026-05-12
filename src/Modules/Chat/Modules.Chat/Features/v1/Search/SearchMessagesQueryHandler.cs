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
    ICurrentUser currentUser)
    : IQueryHandler<SearchMessagesQuery, ReadOnlyCollection<MessageDto>>
{
    public async ValueTask<ReadOnlyCollection<MessageDto>> Handle(
        SearchMessagesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Channel scoping: collect the channel ids the caller is a member of (and, if a specific
        // channel was requested, intersect with that). We then filter the search to those ids —
        // this is the leakage guard, so cross-channel search results never reach non-members.
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

        // FormattedString interpolation is parameterized — `to_tsquery` gets a sanitized literal,
        // not raw SQL. Use `websearch_to_tsquery` so the caller can write natural search syntax
        // (quoted phrases, OR, -exclude) without us pre-processing it.
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

        return rows.Select(m => m.ToDto()).ToList().AsReadOnly();
    }
}
