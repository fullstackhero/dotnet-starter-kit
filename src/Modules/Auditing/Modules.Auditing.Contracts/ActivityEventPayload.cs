namespace FSH.Modules.Auditing.Contracts;

public sealed record ActivityEventPayload(
    ActivityKind Kind,
    string Name,                 // route template, command/query name, job id
    int? StatusCode,
    int DurationMs,
    BodyCapture Captured,        // Request/Response/Both/None
    int RequestSize,
    int ResponseSize,
    object? RequestPreview,      // truncated/filtered snapshot (JSON-friendly)
    object? ResponsePreview
);