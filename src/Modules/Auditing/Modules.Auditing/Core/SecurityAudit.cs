using FSH.Modules.Auditing.Contracts;

namespace FSH.Modules.Auditing;

public sealed class SecurityAudit : ISecurityAudit
{
    private readonly IAuditClient _audit;
    public SecurityAudit(IAuditClient audit) => _audit = audit;

    public ValueTask LoginSucceededAsync(string userId, string userName, string clientId, string ip, string userAgent, CancellationToken ct = default)
        => _audit.WriteSecurityAsync(SecurityAction.LoginSucceeded,
            subjectId: userId, clientId: clientId, authMethod: "Password", reasonCode: "", claims: new Dictionary<string, object?>
            { ["ip"] = ip, ["userAgent"] = userAgent },
            severity: AuditSeverity.Information, source: "Identity", ct);

    public ValueTask LoginFailedAsync(string subjectIdOrName, string clientId, string reason, string ip, CancellationToken ct = default)
        => _audit.WriteSecurityAsync(SecurityAction.LoginFailed,
            subjectId: subjectIdOrName, clientId: clientId, authMethod: "Password", reasonCode: reason,
            claims: new Dictionary<string, object?> { ["ip"] = ip },
            severity: AuditSeverity.Warning, source: "Identity", ct);

    public ValueTask TokenIssuedAsync(string userId, string userName, string clientId, string tokenFingerprint, DateTime expiresUtc, CancellationToken ct = default)
        => _audit.WriteSecurityAsync(SecurityAction.TokenIssued,
            subjectId: userId, clientId: clientId, authMethod: "Password", reasonCode: "",
            claims: new Dictionary<string, object?> { ["fingerprint"] = tokenFingerprint, ["expiresAt"] = expiresUtc },
            severity: AuditSeverity.Information, source: "Identity", ct);

    public ValueTask TokenRevokedAsync(string userId, string clientId, string reason, CancellationToken ct = default)
        => _audit.WriteSecurityAsync(SecurityAction.TokenRevoked,
            subjectId: userId, clientId: clientId, authMethod: "", reasonCode: reason, claims: null,
            severity: AuditSeverity.Information, source: "Identity", ct);

    public ValueTask ImpersonationStartedAsync(string actorUserId, string actorTenantId, string targetUserId, string targetTenantId, string clientId, string ip, string userAgent, string reason, CancellationToken ct = default)
        => _audit.WriteSecurityAsync(SecurityAction.ImpersonationStarted,
            subjectId: actorUserId, clientId: clientId, authMethod: "Impersonation", reasonCode: reason,
            claims: new Dictionary<string, object?>
            {
                ["actorTenant"] = actorTenantId,
                ["targetUser"] = targetUserId,
                ["targetTenant"] = targetTenantId,
                ["ip"] = ip,
                ["userAgent"] = userAgent
            },
            severity: AuditSeverity.Warning, source: "Identity", ct);

    public ValueTask ImpersonationEndedAsync(string actorUserId, string actorTenantId, string targetUserId, string targetTenantId, string clientId, CancellationToken ct = default)
        => _audit.WriteSecurityAsync(SecurityAction.ImpersonationEnded,
            subjectId: actorUserId, clientId: clientId, authMethod: "Impersonation", reasonCode: "",
            claims: new Dictionary<string, object?>
            {
                ["actorTenant"] = actorTenantId,
                ["targetUser"] = targetUserId,
                ["targetTenant"] = targetTenantId
            },
            severity: AuditSeverity.Information, source: "Identity", ct);
}