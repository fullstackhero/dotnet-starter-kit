using System.Globalization;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Messages;

/// <summary>
/// Chronological ordering and cursor paging for messages, in the way each provider actually sorts
/// Guid v7 ids.
/// </summary>
/// <remarks>
/// PostgreSQL's <c>uuid</c> compares in byte order, so a v7 id's leading timestamp makes
/// <c>Id DESC</c> equal to newest-first. SQL Server's <c>uniqueidentifier</c> compares the last six
/// bytes first, which reorders pages arbitrarily; there it sorts on the persisted
/// <c>char(36)</c> sort key configured by <see cref="ChatDbContext"/> instead.
/// </remarks>
internal static class MessageOrdering
{
    /// <summary>Newest-first, using whichever key sorts chronologically on this provider.</summary>
    public static IQueryable<Message> OrderByNewest(this IQueryable<Message> source, DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        return db.Database.IsSqlServer()
            ? source.OrderByDescending(m => EF.Property<string>(m, ChatDbContext.MessageSortKey))
            : source.OrderByDescending(m => m.Id);
    }

    /// <summary>Restricts to messages strictly older than <paramref name="beforeId"/>.</summary>
    public static IQueryable<Message> WhereOlderThan(this IQueryable<Message> source, DbContext db, Guid beforeId)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (!db.Database.IsSqlServer())
        {
            return source.Where(m => m.Id.CompareTo(beforeId) < 0);
        }

        // Compare on the same key the ordering uses, so the cursor lands on the page boundary the
        // caller actually saw. CONVERT(char(36), id) is uppercase, which Guid "D" format matches
        // once upper-cased.
        string cursor = beforeId.ToString("D", CultureInfo.InvariantCulture).ToUpperInvariant();
        return source.Where(m => EF.Property<string>(m, ChatDbContext.MessageSortKey).CompareTo(cursor) < 0);
    }
}
