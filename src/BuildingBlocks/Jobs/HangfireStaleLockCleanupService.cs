using System.Data.Common;
using FSH.Framework.Shared.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FSH.Framework.Jobs;

/// <summary>
/// Best-effort cleanup of stale Hangfire locks from crashed instances.
/// Runs as a BackgroundService so it never blocks application startup.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by DI via AddHostedService")]
internal sealed class HangfireStaleLockCleanupService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HangfireStaleLockCleanupService> _logger;

    public HangfireStaleLockCleanupService(
        IConfiguration configuration,
        ILogger<HangfireStaleLockCleanupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Both statements are compile-time constants chosen by provider in CreateCleanup; no value reaches the command text.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short delay to let Hangfire initialize its schema first
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

        var dbOptions = _configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>();
        if (dbOptions is null)
        {
            return;
        }

        (DbConnection Connection, string Sql)? cleanup = CreateCleanup(dbOptions);
        if (cleanup is not { } target)
        {
            return;
        }

        try
        {
            await using DbConnection connection = target.Connection;
            await connection.OpenAsync(stoppingToken).ConfigureAwait(false);

            await using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = target.Sql;

            int deleted = await cmd.ExecuteNonQueryAsync(stoppingToken).ConfigureAwait(false);
            if (deleted > 0)
            {
                _logger.LogWarning("Cleaned up {Count} stale Hangfire locks", deleted);
            }
        }
        // Best-effort cleanup: table may not exist yet on first startup, or DB may be temporarily unreachable
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not cleanup stale Hangfire locks (table may not exist yet)");
        }
    }

    /// <summary>
    /// Builds the provider-specific connection and DELETE. Hangfire's two storage providers name
    /// the lock table and its timestamp column differently — lowercase <c>hangfire.lock.acquired</c>
    /// on PostgreSQL, <c>[HangFire].[Lock].[CreatedAt]</c> on SQL Server.
    /// </summary>
    private static (DbConnection Connection, string Sql)? CreateCleanup(DatabaseOptions dbOptions)
    {
        if (dbOptions.Provider.Equals(DbProviders.PostgreSQL, StringComparison.OrdinalIgnoreCase))
        {
            return (
                new NpgsqlConnection(dbOptions.ConnectionString),
                "DELETE FROM hangfire.lock WHERE acquired < NOW() - INTERVAL '5 minutes'");
        }

        if (dbOptions.Provider.Equals(DbProviders.MSSQL, StringComparison.OrdinalIgnoreCase))
        {
            return (
                new SqlConnection(dbOptions.ConnectionString),
                "DELETE FROM [HangFire].[Lock] WHERE [CreatedAt] < DATEADD(minute, -5, SYSUTCDATETIME())");
        }

        return null;
    }
}
