using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

/// <summary>
/// Resolves the effective limit for a given tenant + resource. Tenant-local overrides on
/// <see cref="AppTenantInfo.QuotaLimits"/> take precedence; otherwise the plan catalog from
/// <see cref="QuotaOptions.Plans"/> is consulted (with fallback to <see cref="QuotaOptions.DefaultPlan"/>).
/// Returns <see cref="long.MaxValue"/> when no limit applies.
/// </summary>
public sealed class QuotaPlanResolver
{
    private readonly QuotaOptions _options;

    public QuotaPlanResolver(QuotaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public long ResolveLimit(AppTenantInfo? tenant, QuotaResource resource)
    {
        if (tenant is not null && tenant.QuotaLimits.TryGetValue(resource, out var tenantLimit))
        {
            return NormalizeLimit(tenantLimit);
        }

        var planName = !string.IsNullOrWhiteSpace(tenant?.Plan) ? tenant!.Plan! : _options.DefaultPlan;

        if (_options.Plans.TryGetValue(planName, out var plan)
            && plan.TryGetValue(resource, out var planLimit))
        {
            return NormalizeLimit(planLimit);
        }

        // Fall back to default plan if the tenant's plan is missing from the catalog.
        if (!string.Equals(planName, _options.DefaultPlan, StringComparison.OrdinalIgnoreCase)
            && _options.Plans.TryGetValue(_options.DefaultPlan, out var defaultPlan)
            && defaultPlan.TryGetValue(resource, out var defaultLimit))
        {
            return NormalizeLimit(defaultLimit);
        }

        return long.MaxValue;
    }

    private static long NormalizeLimit(long value) => value < 0 ? long.MaxValue : value;
}
