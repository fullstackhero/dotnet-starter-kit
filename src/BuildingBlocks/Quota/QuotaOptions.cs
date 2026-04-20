using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

/// <summary>
/// Quota plan catalog. Tenants reference a plan by name via <c>AppTenantInfo.Plan</c>; the limits
/// attached to that plan are used when the tenant has no per-tenant override. A tenant's own
/// <c>QuotaLimits</c> map takes precedence over the plan defaults when present.
/// </summary>
public sealed class QuotaOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Redis connection string. When empty, the in-memory quota service is used instead (suitable
    /// for development/tests only — counters are per-process and not shared across instances).
    /// </summary>
    public string? Redis { get; set; }

    public string DefaultPlan { get; set; } = "free";

    /// <summary>Plan name → per-resource limit map. Use -1 or long.MaxValue for "unlimited".</summary>
    public Dictionary<string, Dictionary<QuotaResource, long>> Plans { get; set; } = new();

    /// <summary>
    /// Whether the root/platform tenant is exempt from quota enforcement. Defaults to true; platform
    /// operators should not be gated by counters that represent customer billing units.
    /// </summary>
    public bool ExemptRootTenant { get; set; } = true;
}
