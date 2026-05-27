namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Deterministic JSON serialization for payloads (camelCase, enum-as-string, stable output).
/// </summary>
public interface IAuditSerializer
{
    string SerializePayload(object payload);
}