using FSH.Modules.Chat.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Search;

/// <summary>
/// Caches whether the SQL Server instance actually has a full-text index on <c>chat.Messages</c>.
/// </summary>
/// <remarks>
/// The migration only creates the full-text catalog and index when
/// <c>SERVERPROPERTY('IsFullTextInstalled')</c> is 1, so an instance without the Full-Text Search
/// feature migrates cleanly but has no index to query. Probing once per process lets message search
/// fall back to a <c>LIKE</c> scan there instead of failing with "Cannot use a CONTAINS or FREETEXT
/// predicate on table 'Messages' because it is not full-text indexed".
/// </remarks>
public sealed class SqlServerFullTextAvailability
{
    private const int Unknown = 0;
    private const int Available = 1;
    private const int Unavailable = 2;

    // Deliberately lock-free: the probe is idempotent and cheap, so a race that runs it twice on
    // startup is harmless and cheaper than holding a disposable lock for the process lifetime.
    private int _state = Unknown;

    /// <summary>
    /// Returns whether <c>chat.Messages</c> carries a full-text index, probing the catalog views on
    /// first call and caching the answer for the lifetime of the process.
    /// </summary>
    public async ValueTask<bool> IsAvailableAsync(ChatDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        int state = Volatile.Read(ref _state);
        if (state != Unknown)
        {
            return state == Available;
        }

        int found = await db.Database
            .SqlQueryRaw<int>(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID('chat.Messages')
                ) THEN 1 ELSE 0 END AS Value
                """)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        bool available = found == 1;
        Volatile.Write(ref _state, available ? Available : Unavailable);
        return available;
    }
}
