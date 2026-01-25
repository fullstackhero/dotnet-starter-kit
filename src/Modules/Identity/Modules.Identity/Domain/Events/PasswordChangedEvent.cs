using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain.Events;

/// <summary>Raised when a user changes their password.</summary>
public sealed record PasswordChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    bool WasReset,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static PasswordChangedEvent Create(string userId, bool wasReset = false, string? correlationId = null, string? tenantId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, wasReset, correlationId, tenantId);
}
