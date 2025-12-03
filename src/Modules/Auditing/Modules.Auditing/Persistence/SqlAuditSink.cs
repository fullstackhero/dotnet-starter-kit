using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Persists audit envelopes into SQL using EF Core.
/// </summary>
public sealed class SqlAuditSink : IAuditSink
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuditSerializer _serializer;
    private readonly ILogger<SqlAuditSink> _log;

    public SqlAuditSink(IServiceScopeFactory scopeFactory, IAuditSerializer serializer, ILogger<SqlAuditSink> log)
        => (_scopeFactory, _serializer, _log) = (scopeFactory, serializer, log);

    public async Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Count == 0) return;

        // Process per-tenant so MultiTenantDbContext has an ambient tenant context.
        foreach (var group in batch.GroupBy(e => e.TenantId))
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();

            var tenantInfo = group.Key is null
                ? await store.GetAsync(MultitenancyConstants.Root.Id).ConfigureAwait(false)
                : await store.GetAsync(group.Key).ConfigureAwait(false);

            if (tenantInfo is null)
            {
                _log.LogWarning("Skipping audit write for tenant {TenantId} because tenant was not found.", group.Key ?? "<null>");
                continue;
            }

            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);

            var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

            var records = group.Select(e => new AuditRecord
            {
                Id = e.Id,
                OccurredAtUtc = e.OccurredAtUtc,
                ReceivedAtUtc = e.ReceivedAtUtc,
                EventType = (int)e.EventType,
                Severity = (byte)e.Severity,
                TenantId = e.TenantId,
                UserId = e.UserId,
                UserName = e.UserName,
                TraceId = e.TraceId,
                SpanId = e.SpanId,
                CorrelationId = e.CorrelationId,
                RequestId = e.RequestId,
                Source = e.Source,
                Tags = (long)e.Tags,
                PayloadJson = _serializer.SerializePayload(e.Payload)
            }).ToList();

            db.AuditRecords.AddRange(records);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            _log.LogInformation("Wrote {Count} audit records for tenant {TenantId}.", records.Count, tenantInfo.Id);
        }
    }
}
