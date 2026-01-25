using FSH.Framework.Core.Domain;
using FSH.Modules.Identity.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace FSH.Modules.Identity.Domain;

public class FshUser : IdentityUser, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Uri? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }

    public string? ObjectId { get; set; }

    /// <summary>Timestamp when the user last changed their password</summary>
    public DateTime LastPasswordChangeDate { get; set; } = DateTime.UtcNow;

    // Navigation property for password history
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

    // IHasDomainEvents implementation
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Records UserRegisteredEvent. Call after user creation.</summary>
    public void RecordRegistered(string? tenantId = null)
    {
        AddDomainEvent(UserRegisteredEvent.Create(
            userId: Id,
            email: Email ?? string.Empty,
            firstName: FirstName,
            lastName: LastName,
            tenantId: tenantId));
    }

    /// <summary>Records PasswordChangedEvent. Call after password change.</summary>
    public void RecordPasswordChanged(bool wasReset = false, string? tenantId = null)
    {
        AddDomainEvent(PasswordChangedEvent.Create(
            userId: Id,
            wasReset: wasReset,
            tenantId: tenantId));
    }

    /// <summary>Sets user to active and records UserActivatedEvent.</summary>
    public void Activate(string? activatedBy = null, string? tenantId = null)
    {
        if (IsActive) return;
        IsActive = true;
        AddDomainEvent(UserActivatedEvent.Create(
            userId: Id,
            activatedBy: activatedBy,
            tenantId: tenantId));
    }

    /// <summary>Sets user to inactive and records UserDeactivatedEvent.</summary>
    public void Deactivate(string? deactivatedBy = null, string? reason = null, string? tenantId = null)
    {
        if (!IsActive) return;
        IsActive = false;
        AddDomainEvent(UserDeactivatedEvent.Create(
            userId: Id,
            deactivatedBy: deactivatedBy,
            reason: reason,
            tenantId: tenantId));
    }

    /// <summary>Records UserRoleAssignedEvent. Call after roles are assigned.</summary>
    public void RecordRolesAssigned(IEnumerable<string> assignedRoles, string? tenantId = null)
    {
        var rolesList = assignedRoles.ToList();
        if (rolesList.Count == 0) return;
        AddDomainEvent(UserRoleAssignedEvent.Create(
            userId: Id,
            assignedRoles: rolesList,
            tenantId: tenantId));
    }
}
