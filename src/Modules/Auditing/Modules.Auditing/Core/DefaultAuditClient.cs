using FSH.Modules.Auditing.Contracts;

namespace FSH.Modules.Auditing;

public sealed class DefaultAuditClient : IAuditClient
{
    public ValueTask WriteEntityChangeAsync(
        string dbContext, string? schema, string table, string entityName, string key,
        EntityOperation operation, IReadOnlyList<PropertyChange> changes,
        string? transactionId = null, AuditSeverity severity = AuditSeverity.Information,
        string? source = null, CancellationToken ct = default)
    {
        return Audit.ForEntityChange(dbContext, schema, table, entityName, key, operation, changes)
            .WithEntityTransactionId(transactionId)
            .WithSource(source)
            .WithSeverity(severity)
            .WriteAsync(ct);
    }

    public ValueTask WriteSecurityAsync(
        SecurityAction action,
        string? subjectId = null, string? clientId = null, string? authMethod = null, string? reasonCode = null,
        IReadOnlyDictionary<string, object?>? claims = null,
        AuditSeverity? severity = null, string? source = null, CancellationToken ct = default)
    {
        return Audit.ForSecurity(action)
            .WithSecurityContext(subjectId, clientId, authMethod, reasonCode, claims)
            .WithSource(source)
            .WithSeverity(severity ?? DefaultSeverity(action))
            .WriteAsync(ct);

        static AuditSeverity DefaultSeverity(SecurityAction a)
            => a is SecurityAction.LoginFailed or SecurityAction.PermissionDenied or SecurityAction.PolicyFailed
               ? AuditSeverity.Warning : AuditSeverity.Information;
    }

    public ValueTask WriteActivityAsync(
        ActivityKind kind, string name, int? statusCode, int durationMs,
        BodyCapture captured = BodyCapture.None, int requestSize = 0, int responseSize = 0,
        object? requestPreview = null, object? responsePreview = null,
        AuditSeverity severity = AuditSeverity.Information, string? source = null, CancellationToken ct = default)
    {
        return Audit.ForActivity(kind, name)
            .WithActivityResult(statusCode, durationMs, captured, requestSize, responseSize, requestPreview, responsePreview)
            .WithSource(source)
            .WithSeverity(severity)
            .WriteAsync(ct);
    }

    public ValueTask WriteExceptionAsync(
        Exception ex, ExceptionArea area = ExceptionArea.None, string? routeOrLocation = null,
        AuditSeverity? severity = null, string? source = null, CancellationToken ct = default)
    {
        return Audit.ForException(ex, area, routeOrLocation, severity)
            .WithSource(source)
            .WriteAsync(ct);
    }
}