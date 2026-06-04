using FSH.Modules.Identity.Contracts.v1.Impersonation;

namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Source of truth for impersonation grant lifecycle (issued → ended/revoked).
/// Backed by an EF entity for persistence + HybridCache for the per-request
/// IsRevokedOrEndedAsync hot path used by the JWT validation hook.
/// </summary>
public interface IImpersonationGrantService
{
    Task<ImpersonationGrantDto> CreateAsync(CreateGrantInput input, CancellationToken ct = default);

    Task<ImpersonationGrantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Mark as naturally ended (operator clicked End in the dashboard). No-op if already terminal.</summary>
    Task<ImpersonationGrantDto?> MarkEndedByJtiAsync(string jti, CancellationToken ct = default);

    Task<ImpersonationGrantDto> RevokeAsync(
        Guid id,
        string revokedByUserId,
        string? revokedByUserName,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Fast-path check used by the JWT validation hook on every request that
    /// carries an act_sub claim. Returns true when the grant is revoked,
    /// explicitly ended, or naturally expired (token-id reuse defense).
    /// </summary>
    Task<bool> IsRevokedOrEndedAsync(string jti, CancellationToken ct = default);

    Task<IReadOnlyList<ImpersonationGrantDto>> ListAsync(
        ImpersonationGrantStatus? status,
        string? impersonatedTenantId,
        string? actorUserId,
        int take,
        CancellationToken ct = default);
}

public sealed record CreateGrantInput(
    string Jti,
    string ActorUserId,
    string? ActorUserName,
    string ActorTenantId,
    string ImpersonatedUserId,
    string? ImpersonatedUserName,
    string ImpersonatedTenantId,
    string Reason,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc,
    string? ClientId,
    string? IpAddress,
    string? UserAgent);
