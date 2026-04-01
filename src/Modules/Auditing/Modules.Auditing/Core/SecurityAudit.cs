using FSH.Modules.Auditing.Contracts;

namespace FSH.Modules.Auditing;

public sealed class SecurityAudit(IAuditClient audit) : ISecurityAudit
{
    public ValueTask LoginSucceededAsync(string userId, string userName, string clientId, string ip, string userAgent, CancellationToken ct = default)
        => audit.WriteSecurityAsync(SecurityAction.LoginSucceeded,
            subjectId: userId, clientId: clientId, authMethod: "Password", reasonCode: "", claims: new Dictionary<string, object?>
            { ["ip"] = ip, ["userAgent"] = userAgent },
            severity: AuditSeverity.Information, source: "Identity", ct);

    public ValueTask LoginFailedAsync(string subjectIdOrName, string clientId, string reason, string ip, CancellationToken ct = default)
        => audit.WriteSecurityAsync(SecurityAction.LoginFailed,
            subjectId: subjectIdOrName, clientId: clientId, authMethod: "Password", reasonCode: reason,
            claims: new Dictionary<string, object?> { ["ip"] = ip },
            severity: AuditSeverity.Warning, source: "Identity", ct);

    public ValueTask TokenIssuedAsync(string userId, string userName, string clientId, string tokenFingerprint, DateTime expiresUtc, CancellationToken ct = default)
        => audit.WriteSecurityAsync(SecurityAction.TokenIssued,
            subjectId: userId, clientId: clientId, authMethod: "Password", reasonCode: "",
            claims: new Dictionary<string, object?> { ["fingerprint"] = tokenFingerprint, ["expiresAt"] = expiresUtc },
            severity: AuditSeverity.Information, source: "Identity", ct);

    public ValueTask TokenRevokedAsync(string userId, string clientId, string reason, CancellationToken ct = default)
        => audit.WriteSecurityAsync(SecurityAction.TokenRevoked,
            subjectId: userId, clientId: clientId, authMethod: "", reasonCode: reason, claims: null,
            severity: AuditSeverity.Information, source: "Identity", ct);
}