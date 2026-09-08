using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Persistence;
using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// EF Core outbox store over the framework-owned <see cref="EventingDbContext"/>.
///
/// Non-generic on purpose: a per-DbContext generic store meant one
/// <c>IOutboxStore</c> registration per module, and .NET DI resolved the last
/// one registered for the entire application (issue #1349).
/// </summary>
public sealed partial class EfCoreOutboxStore : IOutboxStore
{
    private readonly EventingDbContext _dbContext;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<EfCoreOutboxStore> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly EventingOptions _options;
    private readonly AmbientDbTransactionRegistry _ambientTransactions;

    public EfCoreOutboxStore(
        EventingDbContext dbContext,
        IEventSerializer serializer,
        ILogger<EfCoreOutboxStore> logger,
        TimeProvider timeProvider,
        IOptions<EventingOptions> options,
        AmbientDbTransactionRegistry ambientTransactions)
    {
        ArgumentNullException.ThrowIfNull(options);
        _dbContext = dbContext;
        _serializer = serializer;
        _logger = logger;
        _timeProvider = timeProvider;
        _options = options.Value;
        _ambientTransactions = ambientTransactions;
    }

    public async Task AddAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        await EnlistInAmbientTransactionAsync(ct).ConfigureAwait(false);

        var payload = _serializer.Serialize(@event);
        var message = new OutboxMessage
        {
            Id = @event.Id,
            CreatedOnUtc = @event.OccurredOnUtc,
            Type = @event.GetType().AssemblyQualifiedName ?? @event.GetType().FullName!,
            Payload = payload,
            TenantId = @event.TenantId,
            CorrelationId = @event.CorrelationId,
            RetryCount = 0,
            IsDead = false
        };

        await _dbContext.Set<OutboxMessage>().AddAsync(message, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        int batchSize,
        string claimedBy,
        TimeSpan lease,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var until = now.Add(lease);

        if (_dbContext.Database.IsSqlServer())
        {
            // SQL Server's equivalent of SKIP LOCKED: READPAST walks past rows another transaction
            // already holds, UPDLOCK takes the update lock up front so two dispatchers cannot both
            // select the same row, and OUTPUT returns the rows this statement actually claimed.
            // The CTE preserves the ORDER BY that a bare UPDATE TOP(n) would not guarantee.
            var sqlServerClaim = $$"""
                WITH c AS (
                    SELECT TOP({3}) *
                    FROM [{{EventingConstants.SchemaName}}].[OutboxMessages] WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE [IsDead] = 0
                      AND [ProcessedOnUtc] IS NULL
                      AND ([NextRetryAt] IS NULL OR [NextRetryAt] <= {0})
                      AND ([ClaimedUntilUtc] IS NULL OR [ClaimedUntilUtc] < {0})
                    ORDER BY [CreatedOnUtc]
                )
                UPDATE c
                SET [ClaimedUntilUtc] = {1}, [ClaimedBy] = {2}
                OUTPUT INSERTED.*
                """;

            return await _dbContext.Set<OutboxMessage>()
                .FromSqlRaw(sqlServerClaim, now, until, claimedBy, batchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        if (!_dbContext.Database.IsNpgsql())
        {
            // No portable SKIP LOCKED outside Postgres and SQL Server. Fall back to an unclaimed
            // read, which is safe only while a single dispatcher instance runs.
            LogClaimUnsupported(_dbContext.Database.ProviderName);
            return await _dbContext.Set<OutboxMessage>()
                .Where(m => !m.IsDead
                    && m.ProcessedOnUtc == null
                    && (m.NextRetryAt == null || m.NextRetryAt <= now))
                .OrderBy(m => m.CreatedOnUtc)
                .Take(batchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        // The inner SELECT ... FOR UPDATE SKIP LOCKED picks rows no other transaction holds and
        // the outer UPDATE stamps the lease on exactly those, in one statement. Two dispatchers
        // racing therefore partition the batch instead of both publishing the same message.
        // The schema name is a compile-time constant; every runtime value is parameterised.
        var sql = $$"""
            UPDATE "{{EventingConstants.SchemaName}}"."OutboxMessages" AS o
            SET "ClaimedUntilUtc" = {1}, "ClaimedBy" = {2}
            FROM (
                SELECT "Id"
                FROM "{{EventingConstants.SchemaName}}"."OutboxMessages"
                WHERE "IsDead" = FALSE
                  AND "ProcessedOnUtc" IS NULL
                  AND ("NextRetryAt" IS NULL OR "NextRetryAt" <= {0})
                  AND ("ClaimedUntilUtc" IS NULL OR "ClaimedUntilUtc" < {0})
                ORDER BY "CreatedOnUtc"
                LIMIT {3}
                FOR UPDATE SKIP LOCKED
            ) AS c
            WHERE o."Id" = c."Id"
            RETURNING o.*
            """;

        return await _dbContext.Set<OutboxMessage>()
            .FromSqlRaw(sql, now, until, claimedBy, batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ProcessedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
        ReleaseClaim(message);
        _dbContext.Set<OutboxMessage>().Update(message);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MarkAsFailedAsync(OutboxMessage message, string error, bool isDead, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.RetryCount++;
        message.LastError = error;
        message.IsDead = isDead;
        // Space out retries with exponential backoff so a persistently failing message doesn't
        // re-fire every dispatch cycle. A dead message won't be retried, so leave it eligible-null.
        message.NextRetryAt = isDead ? null : _timeProvider.GetUtcNow().UtcDateTime.Add(BackoffFor(message.RetryCount));
        ReleaseClaim(message);
        _dbContext.Set<OutboxMessage>().Update(message);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetDeadLetteredAsync(int max, CancellationToken ct = default)
    {
        if (max <= 0) max = 100;
        return await _dbContext.Set<OutboxMessage>()
            .Where(m => m.IsDead)
            .OrderBy(m => m.CreatedOnUtc)
            .Take(max)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> RedriveDeadLettersAsync(IReadOnlyCollection<Guid>? ids, CancellationToken ct = default)
    {
        var query = _dbContext.Set<OutboxMessage>().Where(m => m.IsDead);
        if (ids is { Count: > 0 })
        {
            query = query.Where(m => ids.Contains(m.Id));
        }

        var dead = await query.ToListAsync(ct).ConfigureAwait(false);
        foreach (var message in dead)
        {
            message.IsDead = false;
            message.RetryCount = 0;
            message.LastError = null;
            message.NextRetryAt = null;
            ReleaseClaim(message);
        }

        if (dead.Count > 0)
        {
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            Telemetry.EventingTelemetry.OutboxRedriven.Add(dead.Count);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Redrove {Count} dead-lettered outbox message(s) for another attempt.", dead.Count);
            }
        }

        return dead.Count;
    }

    /// <summary>
    /// Joins the caller's transaction when there is one, so the outbox row commits or rolls back
    /// with the business data it accompanies. Without this the row is its own transaction and a
    /// crash between the two writes either loses the event or publishes one for work that was
    /// rolled back.
    ///
    /// Only possible because every context in the scope shares one DbConnection
    /// (<see cref="IScopedDbConnectionProvider"/>) — EF Core cannot enlist a context in a
    /// transaction opened on a different connection. With no ambient transaction this is a no-op
    /// and the write behaves exactly as before.
    /// </summary>
    private async Task EnlistInAmbientTransactionAsync(CancellationToken ct)
    {
        var ambient = _ambientTransactions.Find(_dbContext.Database.GetDbConnection());
        var current = _dbContext.Database.CurrentTransaction;

        if (ambient is null)
        {
            // The transaction we previously joined has since completed; drop the stale handle so
            // this write opens its own transaction instead of failing on a disposed one.
            if (current is not null)
            {
                await _dbContext.Database.UseTransactionAsync(null, ct).ConfigureAwait(false);
            }

            return;
        }

        if (!ReferenceEquals(current?.GetDbTransaction(), ambient))
        {
            await _dbContext.Database.UseTransactionAsync(ambient, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Drops the lease once a message reaches a terminal state for this attempt. Leaving a stale
    /// lease on a retryable row would keep it invisible to every dispatcher until it expired.
    /// </summary>
    private static void ReleaseClaim(OutboxMessage message)
    {
        message.ClaimedUntilUtc = null;
        message.ClaimedBy = null;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Provider {Provider} does not support SKIP LOCKED; outbox claiming is disabled. Run a single dispatcher instance.")]
    private partial void LogClaimUnsupported(string? provider);

    private TimeSpan BackoffFor(int retryCount)
    {
        var baseSeconds = _options.OutboxRetryBaseDelaySeconds > 0 ? _options.OutboxRetryBaseDelaySeconds : 30;
        var maxSeconds = _options.OutboxRetryMaxDelaySeconds > 0 ? _options.OutboxRetryMaxDelaySeconds : 3600;
        // retryCount is 1 after the first failure → first backoff is exactly baseSeconds.
        var exponent = Math.Min(retryCount - 1, 30); // clamp so the shift can't overflow
        var seconds = Math.Min((double)baseSeconds * Math.Pow(2, exponent), maxSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
