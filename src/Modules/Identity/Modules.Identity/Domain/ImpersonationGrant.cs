using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain;

/// <summary>
/// Server-side record of a single impersonation session. Created on Start, mutated
/// on Revoke or natural End, and looked up by jti on every authenticated request
/// that carries an act_sub claim. The cached "revoked or ended" check makes this
/// effectively a revocation list for impersonation tokens — which would otherwise
/// be impossible since JWTs aren't natively revocable.
///
/// Implements <see cref="IGlobalEntity"/> to opt out of the auto-applied
/// tenant filter — cross-tenant impersonations span tenants, so the shadow
/// TenantId column doesn't map cleanly. Tenant access is controlled in the
/// query layer via explicit filters on ActorTenantId / ImpersonatedTenantId.
/// </summary>
public class ImpersonationGrant : IGlobalEntity
{
    public Guid Id { get; private set; }

    /// <summary>JWT id (jti claim) of the issued impersonation token. Unique.</summary>
    public string Jti { get; private set; } = default!;

    public string ActorUserId { get; private set; } = default!;
    public string? ActorUserName { get; private set; }
    public string ActorTenantId { get; private set; } = default!;

    public string ImpersonatedUserId { get; private set; } = default!;
    public string? ImpersonatedUserName { get; private set; }
    public string ImpersonatedTenantId { get; private set; } = default!;

    public string Reason { get; private set; } = string.Empty;

    public DateTime StartedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>Set when the operator clicks End-impersonation. Tokens still expire naturally even if null.</summary>
    public DateTime? EndedAtUtc { get; private set; }

    /// <summary>Set when an operator revokes the grant explicitly. Distinct from EndedAtUtc.</summary>
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByUserId { get; private set; }
    public string? RevokedByUserName { get; private set; }
    public string? RevokeReason { get; private set; }

    public string? ClientId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private ImpersonationGrant() { } // EF Core

    public static ImpersonationGrant Create(
        Guid id,
        string jti,
        string actorUserId,
        string? actorUserName,
        string actorTenantId,
        string impersonatedUserId,
        string? impersonatedUserName,
        string impersonatedTenantId,
        string reason,
        DateTime startedAtUtc,
        DateTime expiresAtUtc,
        string? clientId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new ImpersonationGrant
        {
            Id = id,
            Jti = jti,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorTenantId = actorTenantId,
            ImpersonatedUserId = impersonatedUserId,
            ImpersonatedUserName = impersonatedUserName,
            ImpersonatedTenantId = impersonatedTenantId,
            Reason = reason,
            StartedAtUtc = startedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            ClientId = clientId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };
    }

    /// <summary>Mark as naturally ended (operator clicked End-impersonation). No-op if already terminal.</summary>
    public void MarkEnded(DateTime endedAtUtc)
    {
        if (IsTerminal) return;
        EndedAtUtc = endedAtUtc;
    }

    /// <summary>Revoke this grant. No-op if already terminal.</summary>
    public void Revoke(DateTime revokedAtUtc, string revokedByUserId, string? revokedByUserName, string? reason)
    {
        if (IsTerminal) return;
        RevokedAtUtc = revokedAtUtc;
        RevokedByUserId = revokedByUserId;
        RevokedByUserName = revokedByUserName;
        RevokeReason = reason;
    }

    public bool IsTerminal => EndedAtUtc.HasValue || RevokedAtUtc.HasValue;
    public bool IsRevoked => RevokedAtUtc.HasValue;
}
