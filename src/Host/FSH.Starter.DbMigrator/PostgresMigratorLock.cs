using System.Globalization;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace FSH.Starter.DbMigrator;

/// <summary>
/// Coordinates concurrent migrator invocations via a Postgres session-level
/// advisory lock plus a wait-for-database backoff loop. Cheap, deterministic,
/// no extra infrastructure required — and the lock auto-releases when the
/// holding connection is disposed (or if the migrator process crashes mid-run).
/// </summary>
internal static partial class PostgresMigratorLock
{
    // Arbitrary 64-bit key — distinct enough to spot in pg_locks views, no
    // semantic meaning beyond "this is the fsh-db-migrator session lock".
    // Held at the server level, so the database it's issued against doesn't
    // affect coordination across the whole instance.
    private const long MigratorAdvisoryLockKey = unchecked((long)0xFE514EC0_DEB1ADE4UL);

    // Parameterised SQL — pg_advisory_lock / pg_advisory_unlock take a bigint,
    // and even though the key is a compile-time constant, using a parameter
    // satisfies CA2100 (review SQL strings for injection risk) without an
    // analyzer suppression.
    private const string AcquireSql = "SELECT pg_advisory_lock(@key)";
    private const string ReleaseSql = "SELECT pg_advisory_unlock(@key)";

    /// <summary>
    /// Polls the configured database until it accepts a connection — handles
    /// Aspire/K8s cold-starts where Postgres takes a few seconds to become
    /// reachable. Exponential backoff up to 10s per attempt, bounded by a
    /// total deadline. Returns when:
    ///   · the database accepts a connection (server is ready), OR
    ///   · the connection fails with SQLSTATE 3D000 "database does not exist"
    ///     — server is reachable; EF will create the database on Migrate.
    /// </summary>
    public static async Task WaitForDatabaseAsync(
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
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                LogPostgresReady(logger, attempt);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "3D000")
            {
                // Server reachable; target database doesn't exist yet. EF
                // will create it on the first MigrateAsync call.
                LogTargetDatabaseMissing(logger, ex);
                return;
            }
            catch (Exception ex) when (ex is NpgsqlException or TimeoutException or SocketException)
            {
                LogPostgresNotReady(logger, ex, attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, 10));
            }
        }

        throw new TimeoutException(string.Create(
            CultureInfo.InvariantCulture,
            $"Postgres did not become reachable within 2 minutes (after {attempt} attempts)."));
    }

    /// <summary>
    /// Acquires the migrator advisory lock — blocks until it's available, so
    /// concurrent migrator invocations serialise automatically. The returned
    /// disposable holds the dedicated lock connection; disposing it (or the
    /// process crashing) releases the lock.
    ///
    /// On a first-run where the target database doesn't exist, falls back to
    /// a no-op holder — there's nothing to protect yet, EF will create the
    /// database on MigrateAsync, and subsequent runs get the real lock.
    /// </summary>
    public static async Task<IAsyncDisposable> AcquireAsync(
        string connectionString,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var conn = new NpgsqlConnection(connectionString);
        try
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (ex.SqlState == "3D000")
        {
            LogSkipLockForMissingDb(logger, ex);
            await conn.DisposeAsync().ConfigureAwait(false);
            return NoopLock.Instance;
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = AcquireSql;
            cmd.Parameters.Add(new NpgsqlParameter("key", NpgsqlDbType.Bigint) { Value = MigratorAdvisoryLockKey });
            LogAcquiringLock(logger, MigratorAdvisoryLockKey);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        LogLockAcquired(logger);

        return new LockHolder(conn, logger);
    }

    private sealed class LockHolder : IAsyncDisposable
    {
        private readonly NpgsqlConnection _conn;
        private readonly ILogger _logger;

        public LockHolder(NpgsqlConnection conn, ILogger logger)
        {
            _conn = conn;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            // Closing the connection auto-releases all session-level advisory
            // locks held on it. The explicit unlock is logging-friendly.
            try
            {
                await using var cmd = _conn.CreateCommand();
                cmd.CommandText = ReleaseSql;
                cmd.Parameters.Add(new NpgsqlParameter("key", NpgsqlDbType.Bigint) { Value = MigratorAdvisoryLockKey });
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                LogLockReleased(_logger);
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
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

    private sealed class NoopLock : IAsyncDisposable
    {
        public static readonly NoopLock Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // LoggerMessage source-gen — compile-time message templates avoid
    // CA1873 (eager argument evaluation) and pair cleanly with S6667
    // (logging in a catch block passes the exception explicitly).

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Postgres ready (attempt {Attempt}).")]
    private static partial void LogPostgresReady(ILogger logger, int attempt);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Postgres reachable but target database doesn't exist yet — EF will create it.")]
    private static partial void LogTargetDatabaseMissing(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information,
        Message = "Postgres not ready (attempt {Attempt}). Retrying in {Delay}s…")]
    private static partial void LogPostgresNotReady(ILogger logger, Exception ex, int attempt, double delay);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information,
        Message = "Target database doesn't exist — skipping advisory lock for first-run; EF will create it.")]
    private static partial void LogSkipLockForMissingDb(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information,
        Message = "Acquiring DbMigrator advisory lock (key {Key:X16})…")]
    private static partial void LogAcquiringLock(ILogger logger, long key);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information,
        Message = "DbMigrator advisory lock acquired.")]
    private static partial void LogLockAcquired(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information,
        Message = "DbMigrator advisory lock released.")]
    private static partial void LogLockReleased(ILogger logger);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug,
        Message = "Advisory unlock failed; connection close will release the lock.")]
    private static partial void LogUnlockBestEffortFail(ILogger logger, Exception ex);
}
