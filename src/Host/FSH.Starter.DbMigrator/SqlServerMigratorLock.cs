using System.Data;
using System.Globalization;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.DbMigrator;

/// <summary>
/// Coordinates concurrent migrator invocations via a SQL Server session-scoped application lock
/// (<c>sp_getapplock</c>) plus a wait-for-database backoff loop — the direct analogue of the
/// Postgres advisory lock. The lock auto-releases when the holding connection is disposed (or if
/// the migrator process crashes mid-run).
/// </summary>
internal sealed partial class SqlServerMigratorLock : IMigratorLock
{
    /// <summary>
    /// Application lock resource name, the SQL Server counterpart of the Postgres advisory key.
    /// Visible in <c>sys.dm_tran_locks</c>.
    /// </summary>
    private const string MigratorLockResource = "fsh-db-migrator";

    /// <summary>Error 4060: "Cannot open database ... requested by the login." — server is up, database is not there.</summary>
    private const int CannotOpenDatabase = 4060;

    /// <summary>Error 911: "Database ... does not exist." — raised when the connection names a missing database.</summary>
    private const int DatabaseDoesNotExist = 911;

    public string ProviderDisplayName => "sql server";

    /// <summary>
    /// Polls until SQL Server accepts a connection. Returns when the server is ready, or when it is
    /// reachable but the target database does not exist yet — EF creates it on the first migrate,
    /// exactly as on Postgres.
    /// </summary>
    public async Task WaitForDatabaseAsync(
        string connectionString,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var delay = TimeSpan.FromSeconds(1);
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(2);
        var attempt = 0;

        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                LogSqlServerReady(logger, attempt);
                return;
            }
            catch (SqlException ex) when (IsMissingDatabase(ex))
            {
                LogTargetDatabaseMissing(logger, ex);
                return;
            }
            catch (Exception ex) when (ex is SqlException or TimeoutException or SocketException)
            {
                LogSqlServerNotReady(logger, ex, attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, 10));
            }
        }

        throw new TimeoutException(string.Create(
            CultureInfo.InvariantCulture,
            $"SQL Server did not become reachable within 2 minutes (after {attempt} attempts)."));
    }

    /// <summary>
    /// Acquires the migrator application lock — blocks until available, so concurrent migrator
    /// invocations serialise automatically.
    /// </summary>
    /// <remarks>
    /// Unlike Postgres advisory locks, <c>sp_getapplock</c> is scoped to a database rather than the
    /// server, so on a first run where the database does not exist yet there is nothing to lock —
    /// the same no-op fallback the Postgres implementation uses applies.
    /// </remarks>
    public async Task<IAsyncDisposable> AcquireAsync(
        string connectionString,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var conn = new SqlConnection(connectionString);
        try
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (IsMissingDatabase(ex))
        {
            LogSkipLockForMissingDb(logger, ex);
            await conn.DisposeAsync().ConfigureAwait(false);
            return NoopMigratorLock.Instance;
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "sp_getapplock";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Resource", MigratorLockResource);
            cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
            // Session owner: the lock lives with the connection, not an ambient transaction, so it
            // survives for the whole migrate and drops if the process dies.
            cmd.Parameters.AddWithValue("@LockOwner", "Session");
            cmd.Parameters.AddWithValue("@LockTimeout", -1);

            LogAcquiringLock(logger, MigratorLockResource);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        LogLockAcquired(logger);
        return new LockHolder(conn, logger);
    }

    public async Task<(string User, string Database)> GetConnectionIdentityAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SUSER_SNAME(), DB_NAME()";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (string.Empty, string.Empty);
        }

        return (reader.GetString(0), reader.GetString(1));
    }

    private static bool IsMissingDatabase(SqlException ex) =>
        ex.Number is CannotOpenDatabase or DatabaseDoesNotExist;

    private sealed class LockHolder : IAsyncDisposable
    {
        private readonly SqlConnection _conn;
        private readonly ILogger _logger;

        public LockHolder(SqlConnection conn, ILogger logger)
        {
            _conn = conn;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            // Closing the connection auto-releases session-owned application locks.
            // The explicit release is logging-friendly.
            try
            {
                await using var cmd = _conn.CreateCommand();
                cmd.CommandText = "sp_releaseapplock";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Resource", MigratorLockResource);
                cmd.Parameters.AddWithValue("@LockOwner", "Session");
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                LogLockReleased(_logger);
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                // Best-effort — the connection drop below releases the lock anyway.
                LogUnlockBestEffortFail(_logger, ex);
            }
            finally
            {
                await _conn.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(EventId = 11, Level = LogLevel.Information,
        Message = "SQL Server ready (attempt {Attempt}).")]
    private static partial void LogSqlServerReady(ILogger logger, int attempt);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information,
        Message = "SQL Server reachable but target database doesn't exist yet — EF will create it.")]
    private static partial void LogTargetDatabaseMissing(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information,
        Message = "SQL Server not ready (attempt {Attempt}). Retrying in {Delay}s…")]
    private static partial void LogSqlServerNotReady(ILogger logger, Exception ex, int attempt, double delay);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information,
        Message = "Target database doesn't exist — skipping application lock for first-run; EF will create it.")]
    private static partial void LogSkipLockForMissingDb(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information,
        Message = "Acquiring DbMigrator application lock ({Resource})…")]
    private static partial void LogAcquiringLock(ILogger logger, string resource);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information,
        Message = "DbMigrator application lock acquired.")]
    private static partial void LogLockAcquired(ILogger logger);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information,
        Message = "DbMigrator application lock released.")]
    private static partial void LogLockReleased(ILogger logger);

    [LoggerMessage(EventId = 18, Level = LogLevel.Debug,
        Message = "Application unlock failed; connection close will release the lock.")]
    private static partial void LogUnlockBestEffortFail(ILogger logger, Exception ex);
}
