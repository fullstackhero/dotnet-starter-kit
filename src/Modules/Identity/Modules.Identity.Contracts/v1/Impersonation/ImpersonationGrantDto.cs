using System.Text.Json.Serialization;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation;

public sealed record ImpersonationGrantDto(
    Guid Id,
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
    DateTime? EndedAtUtc,
    DateTime? RevokedAtUtc,
    string? RevokedByUserId,
    string? RevokedByUserName,
    string? RevokeReason,
    ImpersonationGrantStatus Status);

// Serialize as a string ("Active"/"Ended"/...) not the int, so consumers get readable, reorder-safe
// values. Mirrors TicketStatus elsewhere.
[JsonConverter(typeof(JsonStringEnumConverter<ImpersonationGrantStatus>))]
public enum ImpersonationGrantStatus
{
    /// <summary>Token is valid and within its lifetime.</summary>
    Active = 0,
    /// <summary>Operator clicked End-impersonation in the dashboard.</summary>
    Ended = 1,
    /// <summary>An operator (possibly different from the actor) revoked the grant.</summary>
    Revoked = 2,
    /// <summary>Token reached its natural expiry without an explicit End.</summary>
    Expired = 3,
}
