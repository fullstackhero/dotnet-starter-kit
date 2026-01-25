using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain.Events;

/// <summary>Raised when a user account is activated.</summary>
public sealed record UserActivatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    string? ActivatedBy,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static UserActivatedEvent Create(string userId, string? activatedBy = null, string? correlationId = null, string? tenantId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, activatedBy, correlationId, tenantId);
}
