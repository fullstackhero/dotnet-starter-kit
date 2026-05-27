using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Shared.Multitenancy;

public interface IAppTenantInfo
{
    string? ConnectionString { get; set; }

    /// <summary>Plan name used to resolve default quota limits (falls back to <c>QuotaOptions.DefaultPlan</c> when null).</summary>
    string? Plan { get; set; }

    /// <summary>Per-tenant quota overrides. When a resource is present here the value wins over plan defaults.</summary>
    Dictionary<QuotaResource, long> QuotaLimits { get; }
}