namespace FSH.Modules.Auditing.Contracts;

public sealed record SecurityEventPayload(
    SecurityAction Action,
    string? SubjectId,
    string? ClientId,
    string? AuthMethod,   // Password, OIDC, etc.
    string? ReasonCode,   // InvalidPassword, LockedOut, etc.
    IReadOnlyDictionary<string, object?>? ClaimsSnapshot
);