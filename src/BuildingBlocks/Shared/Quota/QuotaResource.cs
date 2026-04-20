namespace FSH.Framework.Shared.Quota;

/// <summary>
/// Resources that can be metered per tenant. Counter-based resources (ApiCalls) are tracked
/// against a billing period; gauge-based resources (StorageBytes, Users, ActiveFeatureFlags)
/// reflect a point-in-time state and are resolved on demand by registered gauge providers.
/// </summary>
public enum QuotaResource
{
    ApiCalls,
    StorageBytes,
    Users,
    ActiveFeatureFlags
}
