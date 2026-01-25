using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain.Events;

/// <summary>Raised when a user session is revoked.</summary>
public sealed record SessionRevokedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    Guid SessionId,
    string? RevokedBy,
    string? Reason,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static SessionRevokedEvent Create(string userId, Guid sessionId, string? revokedBy = null, string? reason = null, string? correlationId = null, string? tenantId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, sessionId, revokedBy, reason, correlationId, tenantId);
}
