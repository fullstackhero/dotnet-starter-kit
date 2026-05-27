namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Enricher that can return a modified event (e.g., fill missing fields, mask payload).
/// </summary>
public interface IAuditMutatingEnricher
{
    AuditEnvelope Enrich(AuditEnvelope envelope);
}