namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Hook to augment events before they are published (e.g., add tenant/user/trace, normalize fields, enforce caps).
/// </summary>
public interface IAuditEnricher
{
    /// <summary>Mutate/augment the event instance prior to serialization/publish.</summary>
    void Enrich(IAuditEvent auditEvent);
}