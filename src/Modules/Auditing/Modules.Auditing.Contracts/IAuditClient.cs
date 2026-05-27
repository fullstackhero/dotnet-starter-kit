namespace FSH.Modules.Auditing.Contracts;

public interface IAuditClient
{
    // Core
    ValueTask WriteEntityChangeAsync(
        string dbContext, string? schema, string table, string entityName, string key,
        EntityOperation operation, IReadOnlyList<PropertyChange> changes,
        string? transactionId = null, AuditSeverity severity = AuditSeverity.Information,
        string? source = null, CancellationToken ct = default);

    ValueTask WriteSecurityAsync(
        SecurityAction action,
        string? subjectId = null, string? clientId = null, string? authMethod = null, string? reasonCode = null,
        IReadOnlyDictionary<string, object?>? claims = null,
        AuditSeverity? severity = null, string? source = null, CancellationToken ct = default);

    ValueTask WriteActivityAsync(
        ActivityKind kind, string name, int? statusCode, int durationMs,
        BodyCapture captured = BodyCapture.None, int requestSize = 0, int responseSize = 0,
        object? requestPreview = null, object? responsePreview = null,
        AuditSeverity severity = AuditSeverity.Information, string? source = null, CancellationToken ct = default);

    ValueTask WriteExceptionAsync(
        Exception ex, ExceptionArea area = ExceptionArea.None, string? routeOrLocation = null,
        AuditSeverity? severity = null, string? source = null, CancellationToken ct = default);
}