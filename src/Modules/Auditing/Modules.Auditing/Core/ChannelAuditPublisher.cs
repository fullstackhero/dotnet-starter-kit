using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;
using System.Threading.Channels;

namespace FSH.Modules.Auditing;

/// <summary>
/// Non-blocking publisher using a bounded channel. Writer is used on request path; reader is drained by a background worker.
/// </summary>
public sealed class ChannelAuditPublisher : IAuditPublisher
{
    private static readonly IAuditScope DefaultScope = new DefaultAuditScope(null, null, null, null, null, null, null, null, AuditTag.None);
    private readonly Channel<AuditEnvelope> _channel;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IAuditScope CurrentScope =>
        _httpContextAccessor.HttpContext?.RequestServices.GetService(typeof(IAuditScope)) as IAuditScope
        ?? DefaultScope;

    public ChannelAuditPublisher(IHttpContextAccessor httpContextAccessor, int capacity = 50_000)
    {
        _httpContextAccessor = httpContextAccessor;

        // Drop oldest to keep latency predictable under pressure.
        _channel = Channel.CreateBounded<AuditEnvelope>(new BoundedChannelOptions(capacity)
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    public ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var scope = CurrentScope;
        var envelope = CreateEnvelope(auditEvent);
        envelope = BackfillScopeContext(envelope, scope);

        return _channel.Writer.TryWrite(envelope)
            ? ValueTask.CompletedTask
            : ValueTask.FromCanceled(ct);
    }

    private static AuditEnvelope CreateEnvelope(IAuditEvent auditEvent)
    {
        if (auditEvent is AuditEnvelope existing)
        {
            return existing;
        }

        return new AuditEnvelope(
            id: Guid.CreateVersion7(),
            occurredAtUtc: auditEvent.OccurredAtUtc,
            receivedAtUtc: DateTime.UtcNow,
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

    internal ChannelReader<AuditEnvelope> Reader => _channel.Reader;
}
