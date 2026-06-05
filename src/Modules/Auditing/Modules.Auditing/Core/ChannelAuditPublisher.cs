using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Channels;

namespace FSH.Modules.Auditing;

/// <summary>
/// Non-blocking publisher with two lanes: a high-throughput default lane
/// (drop-oldest under pressure) and a compliance-grade security lane
/// (bounded but back-pressures on full). Both lanes are drained by a
/// single background worker that prefers the security lane.
/// </summary>
public sealed class ChannelAuditPublisher : IAuditPublisher
{
    private static readonly IAuditScope DefaultScope = new DefaultAuditScope(null, null, null, null, null, null, null, null, AuditTag.None);
    private readonly Channel<AuditEnvelope> _default;
    private readonly Channel<AuditEnvelope> _security;
    private readonly int _defaultCapacity;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly TimeProvider _timeProvider;

    public IAuditScope CurrentScope =>
        _httpContextAccessor.HttpContext?.RequestServices.GetService(typeof(IAuditScope)) as IAuditScope
        ?? DefaultScope;

    public ChannelAuditPublisher(
        IHttpContextAccessor httpContextAccessor,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        TimeProvider timeProvider,
        int capacity = 50_000,
        int securityCapacity = 50_000)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantAccessor = tenantAccessor;
        _timeProvider = timeProvider;
        _defaultCapacity = capacity;

        // Default lane: drop oldest to keep latency predictable under
        // pressure. Acceptable for activity / entity-change events.
        _default = Channel.CreateBounded<AuditEnvelope>(new BoundedChannelOptions(capacity)
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Security lane: never drop. If the sink is wedged, publishers
        // back-pressure until the queue drains. Compliance-grade events
        // (login outcomes, permission changes, impersonation) ride here.
        _security = Channel.CreateBounded<AuditEnvelope>(new BoundedChannelOptions(securityCapacity)
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var scope = CurrentScope;
        var envelope = CreateEnvelope(auditEvent);
        envelope = BackfillScopeContext(envelope, scope);
        envelope = BackfillAmbientContext(envelope);

        var typeTag = new KeyValuePair<string, object?>("event_type", envelope.EventType.ToString());
        AuditingTelemetry.Published.Add(1, typeTag);

        if (envelope.EventType == AuditEventType.Security)
        {
            // WriteAsync awaits when the security channel is full — that's
            // the entire point of this lane: we'd rather slow the request
            // than drop a compliance-relevant event.
            await _security.Writer.WriteAsync(envelope, ct).ConfigureAwait(false);
            return;
        }

        // DropOldest never returns false from TryWrite, so approximate drops by comparing
        // reader depth to capacity before writing — racy under multi-writer, but rate is what matters.
        if (_default.Reader.Count >= _defaultCapacity)
        {
            AuditingTelemetry.Dropped.Add(1, typeTag);
        }
        _default.Writer.TryWrite(envelope);
    }

    private AuditEnvelope CreateEnvelope(IAuditEvent auditEvent)
    {
        if (auditEvent is AuditEnvelope existing)
        {
            return existing;
        }

        return new AuditEnvelope(
            id: Guid.CreateVersion7(),
            occurredAtUtc: auditEvent.OccurredAtUtc,
            receivedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            eventType: auditEvent.EventType,
            severity: auditEvent.Severity,
            tenantId: auditEvent.TenantId,
            userId: auditEvent.UserId,
            userName: auditEvent.UserName,
            traceId: auditEvent.TraceId,
            spanId: auditEvent.SpanId,
            correlationId: auditEvent.CorrelationId,
            requestId: auditEvent.RequestId,
            source: auditEvent.Source,
            tags: auditEvent.Tags,
            payload: auditEvent.Payload);
    }

    private static AuditEnvelope BackfillScopeContext(AuditEnvelope env, IAuditScope scope)
    {
        bool needsTenantBackfill = string.IsNullOrWhiteSpace(env.TenantId);
        bool needsUserBackfill = string.IsNullOrWhiteSpace(env.UserId) && scope.UserId is not null;

        if (!needsTenantBackfill && !needsUserBackfill)
        {
            return env;
        }

        return new AuditEnvelope(
            id: env.Id,
            occurredAtUtc: env.OccurredAtUtc,
            receivedAtUtc: env.ReceivedAtUtc,
            eventType: env.EventType,
            severity: env.Severity,
            tenantId: needsTenantBackfill ? scope.TenantId : env.TenantId,
            userId: needsUserBackfill ? scope.UserId : env.UserId,
            userName: needsUserBackfill ? scope.UserName ?? env.UserName : env.UserName,
            traceId: env.TraceId,
            spanId: env.SpanId,
            correlationId: env.CorrelationId,
            requestId: env.RequestId,
            source: env.Source,
            tags: env.Tags,
            payload: env.Payload);
    }

    /// <summary>
    /// Last-resort enrichment for envelopes published outside an HTTP
    /// request — typically the SaveChanges interceptor running inside a
    /// Hangfire job. Reads tenant from the ambient Finbuckle accessor and
    /// trace info from <see cref="Activity.Current"/>; user attribution
    /// stays whatever the scope provided (the activator-set
    /// <c>ICurrentUser</c> is scoped, so the publisher can't see it).
    /// </summary>
    private AuditEnvelope BackfillAmbientContext(AuditEnvelope env)
    {
        bool needTenant = string.IsNullOrWhiteSpace(env.TenantId);
        bool needTrace = string.IsNullOrWhiteSpace(env.TraceId);
        bool needSpan = string.IsNullOrWhiteSpace(env.SpanId);

        if (!needTenant && !needTrace && !needSpan) return env;

        var ambientTenant = needTenant
            ? _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            : null;
        var activity = Activity.Current;

        return new AuditEnvelope(
            id: env.Id,
            occurredAtUtc: env.OccurredAtUtc,
            receivedAtUtc: env.ReceivedAtUtc,
            eventType: env.EventType,
            severity: env.Severity,
            tenantId: needTenant ? ambientTenant ?? env.TenantId : env.TenantId,
            userId: env.UserId,
            userName: env.UserName,
            traceId: needTrace ? activity?.TraceId.ToString() ?? env.TraceId : env.TraceId,
            spanId: needSpan ? activity?.SpanId.ToString() ?? env.SpanId : env.SpanId,
            correlationId: env.CorrelationId,
            requestId: env.RequestId,
            source: env.Source,
            tags: env.Tags,
            payload: env.Payload);
    }

    /// <summary>Default-lane reader. Drained second by the worker.</summary>
    internal ChannelReader<AuditEnvelope> Reader => _default.Reader;

    /// <summary>Security-lane reader. Drained first by the worker.</summary>
    internal ChannelReader<AuditEnvelope> SecurityReader => _security.Reader;
}
