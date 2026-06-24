namespace FSH.Modules.Identity;

/// <summary>
/// Login-side view of the tenant billing grace period (config section <c>"Billing"</c>). A tenant
/// whose subscription has lapsed can still authenticate until <c>ValidUpto + GracePeriodDays</c>.
/// </summary>
public sealed class TenantGraceOptions
{
    public const string SectionName = "Billing";

    public int GracePeriodDays { get; set; } = 7;
}
