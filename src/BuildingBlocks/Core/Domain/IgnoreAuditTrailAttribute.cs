using System;

namespace FSH.Framework.Core.Domain;

/// <summary>
/// Marks an entity class so that changes to it are NOT recorded in the audit trail.
/// Apply to high-frequency internal entities like outbox messages, inbox messages, or session tokens
/// that do not require compliance auditing.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class IgnoreAuditTrailAttribute : Attribute { }
