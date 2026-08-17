using FSH.Framework.Core.Exceptions;
using FSH.Modules.Auditing.Contracts;
using System.Diagnostics;

namespace FSH.Modules.Auditing;

/// <summary>
/// Fluent entry-point to create and publish audit events.
/// Configure once at startup with a publisher, serializer, and optional enrichers.
/// </summary>
public static class Audit
{
    public static IAuditPublisher Publisher { get; private set; } = new NoopPublisher();
    public static IAuditSerializer Serializer { get; private set; } = new SystemTextJsonAuditSerializer();

    // Immutable snapshot swapped atomically by Configure(). Readers take one volatile read and
    // enumerate that — never the live collection — so reconfig can't throw "Collection modified".
    private static volatile IAuditEnricher[] _enrichers = [];

    public static void Configure(
        IAuditPublisher publisher,
        IAuditSerializer? serializer = null,
        IEnumerable<IAuditEnricher>? enrichers = null)
    {
        Publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        if (serializer is not null) Serializer = serializer;
        _enrichers = enrichers is not null ? [.. enrichers] : [];
    }

    // --- Factory methods -------------------------------------------------------

    public static Builder ForEntityChange(
        string dbContext, string? schema, string table, string entityName, string key,
        EntityOperation operation, IEnumerable<PropertyChange> changes)
        => new Builder(
            eventType: AuditEventType.EntityChange,
            severity: AuditSeverity.Information,
            payload: new EntityChangeEventPayload(dbContext, schema, table, entityName, key, operation, changes.ToArray(), TransactionId: null));

    public static Builder ForSecurity(SecurityAction action)
        => new Builder(
            eventType: AuditEventType.Security,
            severity: action is SecurityAction.LoginFailed or SecurityAction.PermissionDenied or SecurityAction.PolicyFailed
                ? AuditSeverity.Warning : AuditSeverity.Information,
            payload: new SecurityEventPayload(action, null, null, null, null, null));

    public static Builder ForActivity(Contracts.ActivityKind kind, string name)
        => new Builder(
            eventType: AuditEventType.Activity,
            severity: AuditSeverity.Information,
            payload: new ActivityEventPayload(kind, name, null, 0, BodyCapture.None, 0, 0, null, null));

    public static Builder ForException(Exception ex, ExceptionArea area = ExceptionArea.None, string? routeOrLocation = null, AuditSeverity? severity = null)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return new Builder(
                eventType: AuditEventType.Exception,
                severity: severity ?? DefaultSeverity(ex),
                payload: new ExceptionEventPayload(area,
                    RealExceptionType(ex).FullName ?? "Exception",
                    ex.Message ?? string.Empty,
                    StackTop(ex, maxFrames: 20),
                    ToDict(ex.Data),
                    routeOrLocation));
    }

    // Localization wrappers (LocalizedKeyNotFoundException, LocalizedUnauthorizedAccessException) exist
    // only to translate the response body; for audit type identity and exceptionType filtering they must
    // present as their BCL base so queries stay stable. CustomException-derived types keep their own identity.
    private static Type RealExceptionType(Exception ex) =>
        ex is ILocalizableMessage and not CustomException && ex.GetType().BaseType is { } baseType
            ? baseType
            : ex.GetType();

    private static AuditSeverity DefaultSeverity(Exception ex)
    {
        if (ex is OperationCanceledException)
            return AuditSeverity.Information;
        if (ex is UnauthorizedAccessException)
            return AuditSeverity.Warning;
        return AuditSeverity.Error;
    }

    private static List<string> StackTop(Exception ex, int maxFrames)
    {
        var frames = new List<string>(maxFrames);
        var trace = new StackTrace(ex, true);
        foreach (var f in trace.GetFrames() ?? Array.Empty<StackFrame>())
        {
            if (frames.Count >= maxFrames) break;
            var method = f.GetMethod();
            var name = method is null ? "<unknown>" : $"{method.DeclaringType?.FullName}.{method.Name}";
            var file = f.GetFileName();
            var line = f.GetFileLineNumber();
            frames.Add(file is null ? name : $"{name} ({file}:{line})");
        }
        return frames;
    }

    private static Dictionary<string, object?>? ToDict(System.Collections.IDictionary? data)
    {
        if (data is null || data.Count == 0) return null;
        var dict = new Dictionary<string, object?>(data.Count);
        foreach (var k in data.Keys)
        {
            var key = k?.ToString() ?? "key";
            dict[key] = data[key];
        }
        return dict;
    }

    // --- Builder ---------------------------------------------------------------

    public sealed class Builder
    {
        private readonly AuditEventType _type;
        private AuditSeverity _severity;
        private object _payload;

        private string? _tenantId;
        private string? _userId;
        private string? _userName;
        private string? _traceId = Activity.Current?.TraceId.ToString();
        private string? _spanId = Activity.Current?.SpanId.ToString();
        private string? _correlationId;
        private string? _requestId;
        private string? _source;
        private AuditTag _tags = AuditTag.None;
        private DateTime _occurredAtUtc = TimeProvider.System.GetUtcNow().UtcDateTime;

        internal Builder(AuditEventType eventType, AuditSeverity severity, object payload)
        {
            _type = eventType;
            _severity = severity;
            _payload = payload;
        }

        public Builder WithSeverity(AuditSeverity severity) { _severity = severity; return this; }
        public Builder WithTenant(string? tenantId) { _tenantId = tenantId; return this; }
        public Builder WithUser(string? userId, string? userName = null) { _userId = userId; _userName = userName; return this; }
        public Builder WithTrace(string? traceId, string? spanId = null) { _traceId = traceId; _spanId = spanId; return this; }
        public Builder WithCorrelation(string? correlationId) { _correlationId = correlationId; return this; }
        public Builder WithRequestId(string? requestId) { _requestId = requestId; return this; }
        public Builder WithSource(string? source) { _source = source; return this; }
        public Builder WithTags(AuditTag tags) { _tags |= tags; return this; }
        public Builder At(DateTime utc) { _occurredAtUtc = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime(); return this; }

        // Typed updaters --------------------------------------------------------

        public Builder WithEntityTransactionId(string? transactionId)
        {
            if (_payload is EntityChangeEventPayload p) _payload = p with { TransactionId = transactionId };
            return this;
        }

        public Builder WithSecurityContext(
            string? subjectId = null,
            string? clientId = null,
            string? authMethod = null,
            string? reasonCode = null,
            IReadOnlyDictionary<string, object?>? claims = null)
        {
            if (_payload is SecurityEventPayload p)
                _payload = p with { SubjectId = subjectId, ClientId = clientId, AuthMethod = authMethod, ReasonCode = reasonCode, ClaimsSnapshot = claims };
            return this;
        }

        public Builder WithActivityResult(
            int? statusCode, int durationMs,
            BodyCapture captured = BodyCapture.None,
            int requestSize = 0, int responseSize = 0,
            object? requestPreview = null, object? responsePreview = null)
        {
            if (_payload is ActivityEventPayload p)
                _payload = p with { StatusCode = statusCode, DurationMs = durationMs, Captured = captured, RequestSize = requestSize, ResponseSize = responseSize, RequestPreview = requestPreview, ResponsePreview = responsePreview };
            return this;
        }

        // Finalize + publish ----------------------------------------------------

        public async ValueTask WriteAsync(CancellationToken ct = default)
        {
            var env = new AuditEnvelope(
                id: Guid.CreateVersion7(),
                occurredAtUtc: _occurredAtUtc,
                receivedAtUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
                eventType: _type,
                severity: _severity,
                tenantId: _tenantId,
                userId: _userId,
                userName: _userName,
                traceId: _traceId,
                spanId: _spanId,
                correlationId: _correlationId,
                requestId: _requestId,
                source: _source,
                tags: _tags,
                payload: _payload
            );

            // Enrich prior to publish — single volatile read, then iterate the immutable snapshot.
            var enrichers = _enrichers;
            foreach (var enricher in enrichers)
                enricher.Enrich(env);

            await Publisher.PublishAsync(env, ct);
        }

    }

    // --- tiny safe defaults so dev builds run ---------------------------------
    private sealed class NoopPublisher : IAuditPublisher
    {
        public IAuditScope CurrentScope { get; } = new DefaultAuditScope(null, null, null, null, null, null, null, null, AuditTag.None);
        public ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default) => ValueTask.CompletedTask;
    }
}