using FSH.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Captures EF Core entity changes at SaveChanges to produce an EntityChange event.
/// </summary>
public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public AuditingSaveChangesInterceptor(IAuditPublisher publisher, TimeProvider timeProvider)
    {
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var ctx = eventData.Context;
        if (ctx is null) return result;

        var entries = ctx.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        if (entries.Length == 0) return result;

        var diffs = EntityDiffBuilder.Build(entries);

        if (diffs.Count > 0)
        {
            foreach (var group in diffs.GroupBy(d => (d.DbContext, d.Schema, d.Table, d.EntityName, d.Key, d.Operation)))
            {
                var payload = new EntityChangeEventPayload(
                    DbContext: group.Key.DbContext,
                    Schema: group.Key.Schema,
                    Table: group.Key.Table,
                    EntityName: group.Key.EntityName,
                    Key: group.Key.Key,
                    Operation: group.Key.Operation,
                    Changes: group.SelectMany(g => g.Changes).ToList(),
                    TransactionId: ctx.Database.CurrentTransaction?.TransactionId.ToString());

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var env = new AuditEnvelope(
                    id: Guid.CreateVersion7(),
                    occurredAtUtc: now,
                    receivedAtUtc: now,
                    eventType: AuditEventType.EntityChange,
                    severity: AuditSeverity.Information,
                    tenantId: null, userId: null, userName: null,
                    traceId: null, spanId: null, correlationId: null, requestId: null,
                    source: ctx.GetType().Name,
                    tags: AuditTag.None,
                    payload: payload);

                await _publisher.PublishAsync(env, cancellationToken);
            }
        }

        return result;
    }
}
