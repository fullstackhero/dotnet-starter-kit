namespace FSH.Modules.Multitenancy;

/// <summary>
/// Tenant-lifecycle billing knobs (config section <c>"Billing"</c>): the plan a tenant falls back to
/// when created without one, and how long past <c>ValidUpto</c> a tenant keeps working before being
/// hard-blocked.
/// </summary>
public sealed class TenantBillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>Plan key assigned when CreateTenant is called without an explicit plan.</summary>
    public string DefaultPlanKey { get; set; } = "free";

    /// <summary>Days past <c>ValidUpto</c> during which requests/logins still succeed.</summary>
    public int GraceWindowDays { get; set; } = 7;
}
