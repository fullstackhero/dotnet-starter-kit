namespace FSH.Modules.Auditing.Contracts;

public interface ISecurityAudit
{
    ValueTask LoginSucceededAsync(string userId, string userName, string clientId, string ip, string userAgent, CancellationToken ct = default);
    ValueTask LoginFailedAsync(string subjectIdOrName, string clientId, string reason, string ip, CancellationToken ct = default);
    ValueTask TokenIssuedAsync(string userId, string userName, string clientId, string tokenFingerprint, DateTime expiresUtc, CancellationToken ct = default);
    ValueTask TokenRevokedAsync(string userId, string clientId, string reason, CancellationToken ct = default);
    ValueTask ImpersonationStartedAsync(string actorUserId, string actorTenantId, string targetUserId, string targetTenantId, string clientId, string ip, string userAgent, string reason, CancellationToken ct = default);
    ValueTask ImpersonationEndedAsync(string actorUserId, string actorTenantId, string targetUserId, string targetTenantId, string clientId, CancellationToken ct = default);
}