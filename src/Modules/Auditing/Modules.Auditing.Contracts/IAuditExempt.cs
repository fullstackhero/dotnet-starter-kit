namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Opt-out marker for the entity-change audit trail. An entity implementing this is skipped by the
/// auditing SaveChanges interceptor, so no property-level diff of it is ever captured or stored.
/// Use it for entities whose values must live in one table only (e.g. confidential reports, health
/// records). <see cref="NoAuditAttribute"/> is unrelated: it only governs HTTP activity auditing.
/// </summary>
#pragma warning disable CA1040 // Marker interface is the intended shape (checked with `is`, not reflection).
public interface IAuditExempt;
#pragma warning restore CA1040
