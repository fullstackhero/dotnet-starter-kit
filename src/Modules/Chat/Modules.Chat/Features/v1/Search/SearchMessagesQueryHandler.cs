using System.Collections.ObjectModel;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence.Providers;
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
    IMediator mediator,
    SqlServerFullTextAvailability fullText)
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

        int offset = (page - 1) * pageSize;

        IQueryable<Message> matches = await BuildSearchQueryAsync(
            query, allowedChannelIds, pageSize, offset, cancellationToken).ConfigureAwait(false);

        var rows = await matches
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

    /// <summary>
    /// How far back the LIKE pass reaches to cover rows SQL Server's full-text crawl has not
    /// indexed yet. Generous enough to absorb a slow crawl under load, short enough that the pass
    /// stays a small scan within the caller's channels.
    /// </summary>
    private const int FullTextCrawlWindowMinutes = 5;

    /// <summary>
    /// Builds the ranked, paged match query for the current provider. Ranking lives inside the raw
    /// SQL on both full-text paths because the rank is not a column of <see cref="Message"/> and so
    /// cannot survive LINQ composition.
    /// </summary>
    private async ValueTask<IQueryable<Message>> BuildSearchQueryAsync(
        SearchMessagesQuery query,
        List<Guid> allowedChannelIds,
        int pageSize,
        int offset,
        CancellationToken cancellationToken)
    {
        if (db.Database.IsNpgsql())
        {
            // Interpolation is parameterized (sanitized literal, not raw SQL); websearch_to_tsquery lets
            // callers use natural syntax (quoted phrases, OR, -exclude) with no pre-processing.
            FormattableString postgres = $@"
SELECT m.*
FROM chat.""Messages"" m
WHERE m.""ChannelId"" = ANY({allowedChannelIds.ToArray()})
  AND m.""DeletedAtUtc"" IS NULL
  AND m.""BodyTsv"" @@ websearch_to_tsquery('english', {query.Q})
ORDER BY ts_rank(m.""BodyTsv"", websearch_to_tsquery('english', {query.Q})) DESC,
         m.""Id"" DESC
LIMIT {pageSize} OFFSET {offset}
";
            return db.Messages.FromSqlInterpolated(postgres);
        }

        if (db.Database.IsSqlServer()
            && await fullText.IsAvailableAsync(db, cancellationToken).ConfigureAwait(false))
        {
            // FREETEXTTABLE gives stemming and a relevance RANK, the closest analogue to
            // websearch_to_tsquery + ts_rank — but SQL Server populates a full-text index
            // asynchronously, so a message sent seconds ago is not in it yet. PostgreSQL's tsvector
            // is a generated column and therefore synchronous, so relying on the index alone would
            // make a just-sent message silently unsearchable here and nowhere else.
            //
            // So: ranked hits from the index, UNIONed with a plain LIKE pass over the crawl window
            // the index cannot have reached yet. Those pending matches are by definition the newest,
            // and are returned first (newest-first) ahead of the ranked ones, which is what someone
            // searching for what they just typed expects. NOT EXISTS keeps a message that is in both
            // sets from appearing twice.
            //
            // STRING_SPLIT keeps the channel list a single parameter rather than an interpolated
            // id list. Derived tables rather than a CTE on purpose: EF wraps a FromSql query in a
            // subselect to resolve the Includes, and `SELECT ... FROM (WITH ... SELECT ...) t` is
            // not valid T-SQL — a CTE here fails at runtime with a 500.
            string channelIds = string.Join(',', allowedChannelIds);
            FormattableString sqlServer = $@"
SELECT m.*
FROM chat.[Messages] m
INNER JOIN (
    SELECT s.[Id], ft.[RANK] AS Rnk, 0 AS Pending
    FROM chat.[Messages] s
    INNER JOIN FREETEXTTABLE(chat.[Messages], [Body], {query.Q}) ft ON ft.[KEY] = s.[Id]
    WHERE s.[ChannelId] IN (SELECT CAST(value AS uniqueidentifier) FROM STRING_SPLIT({channelIds}, ','))
      AND s.[DeletedAtUtc] IS NULL
    UNION ALL
    SELECT s.[Id], 0 AS Rnk, 1 AS Pending
    FROM chat.[Messages] s
    WHERE s.[ChannelId] IN (SELECT CAST(value AS uniqueidentifier) FROM STRING_SPLIT({channelIds}, ','))
      AND s.[DeletedAtUtc] IS NULL
      AND s.[CreatedAtUtc] > DATEADD(minute, {-FullTextCrawlWindowMinutes}, SYSUTCDATETIME())
      AND s.[Body] LIKE {'%' + query.Q + '%'}
      AND NOT EXISTS (
          SELECT 1 FROM FREETEXTTABLE(chat.[Messages], [Body], {query.Q}) ft2
          WHERE ft2.[KEY] = s.[Id]
      )
) h ON h.[Id] = m.[Id]
ORDER BY h.Pending DESC, h.Rnk DESC, m.[IdSort] DESC
OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
";
            return db.Messages.FromSqlInterpolated(sqlServer);
        }

        // No full-text index available: a portable substring scan, ordered newest-first because
        // there is no relevance score to rank by. Composed in LINQ so it works on any provider.
        return db.Messages
            .Where(m => allowedChannelIds.Contains(m.ChannelId) && m.DeletedAtUtc == null)
            .WhereLike(db.Database, $"%{query.Q}%", m => m.Body)
            .OrderByDescending(m => m.Id)
            .Skip(offset)
            .Take(pageSize);
    }
}
