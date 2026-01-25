using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain.Events;

/// <summary>Raised when a new user registers in the system.</summary>
public sealed record UserRegisteredEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static UserRegisteredEvent Create(string userId, string email, string? firstName = null, string? lastName = null, string? correlationId = null, string? tenantId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, email, firstName, lastName, correlationId, tenantId);
}
