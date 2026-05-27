using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace FSH.Framework.Web.FeatureFlags;

/// <summary>
/// A feature filter that enables/disables features based on the current tenant.
/// Configure in appsettings.json with allowed tenant IDs.
/// </summary>
[FilterAlias("Tenant")]
public sealed class TenantFeatureFilter(
    IHttpContextAccessor httpContextAccessor,
    IMultiTenantContextAccessor<AppTenantInfo>? tenantContextAccessor = null) : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tenantId = tenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            // Fall back to header if tenant context not yet resolved
            tenantId = httpContextAccessor.HttpContext?.Request.Headers[MultitenancyConstants.Identifier].ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult(false);
        }

        var allowedTenants = context.Parameters.GetSection("AllowedTenants").Get<string[]>() ?? [];
        var result = allowedTenants.Contains(tenantId, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(result);
    }
}
