namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Retention windows applied by the daily audit purge job. Each event type
/// can be retained independently — security and exception events are kept
/// long for compliance, activity is kept short to keep the table tractable.
/// Defaults to "off" via the <see cref="Enabled"/> flag so installations
/// opt in deliberately.
/// </summary>
public sealed class AuditRetentionOptions
{
    /// <summary>Master switch for the retention purge job.</summary>
    public bool Enabled { get; set; }

    /// <summary>Retention for activity (HTTP/job/command) events.</summary>
    public int ActivityRetentionDays { get; set; } = 30;

    /// <summary>Retention for entity-change events.</summary>
    public int EntityChangeRetentionDays { get; set; } = 90;

    /// <summary>Retention for security events. Compliance-friendly default.</summary>
    public int SecurityRetentionDays { get; set; } = 365;

    /// <summary>Retention for exception events.</summary>
    public int ExceptionRetentionDays { get; set; } = 180;

    /// <summary>
    /// Maximum rows deleted per <c>ExecuteDeleteAsync</c> call. Smaller
    /// batches lower lock pressure on Postgres at the cost of more round
    /// trips. The job loops until it deletes fewer than the batch size.
    /// </summary>
    public int DeleteBatchSize { get; set; } = 5_000;

    /// <summary>
    /// Hangfire cron expression for the purge job. Daily at 03:30 UTC by
    /// default — off-hours for most timezones, after most reporting jobs.
    /// </summary>
    public string Cron { get; set; } = "30 3 * * *";
}
