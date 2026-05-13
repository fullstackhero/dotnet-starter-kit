using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FSH.Starter.DbMigrator;

/// <summary>
/// Coordinates concurrent migrator invocations via a Postgres session-level
/// advisory lock plus a wait-for-database backoff loop. Cheap, deterministic,
/// no extra infrastructure required — and the lock auto-releases when the
/// holding connection is disposed (or if the migrator process crashes mid-run).
/// </summary>
internal static class PostgresMigratorLock
{
    // Arbitrary 64-bit key — distinct enough to spot in pg_locks views, no
    // semantic meaning beyond "this is the fsh-db-migrator session lock".
    // Held at the server level, so the database it's issued against doesn't
    // affect coordination across the whole instance.
    private const long MigratorAdvisoryLockKey = unchecked((long)0xFE514EC0_DEB1ADE4UL);

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
                logger.LogInformation("Postgres ready (attempt {Attempt}).", attempt);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "3D000")
            {
                // Server reachable; target database doesn't exist yet. EF
                // will create it on the first MigrateAsync call.
                logger.LogInformation(
                    "Postgres reachable but target database doesn't exist yet — EF will create it.");
                return;
            }
            catch (Exception ex) when (ex is NpgsqlException or TimeoutException or SocketException)
            {
                logger.LogInformation(
                    "Postgres not ready (attempt {Attempt}, {Error}). Retrying in {Delay:0.0}s…",
                    attempt, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, 10));
            }
        }

        throw new TimeoutException(
            $"Postgres did not become reachable within 2 minutes (after {attempt} attempts).");
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
            logger.LogInformation(
                "Target database doesn't exist — skipping advisory lock for first-run; EF will create it.");
            await conn.DisposeAsync().ConfigureAwait(false);
            return NoopLock.Instance;
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT pg_advisory_lock({MigratorAdvisoryLockKey})";
            logger.LogInformation("Acquiring DbMigrator advisory lock (key {Key:X16})…", MigratorAdvisoryLockKey);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        logger.LogInformation("DbMigrator advisory lock acquired.");

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
                cmd.CommandText = $"SELECT pg_advisory_unlock({MigratorAdvisoryLockKey})";
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                _logger.LogInformation("DbMigrator advisory lock released.");
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
            {
                // Best-effort — the connection drop below releases the lock anyway.
                _logger.LogDebug(ex, "Advisory unlock failed; connection close will release the lock.");
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
}
