using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FSH.Modules.Multitenancy;

/// <summary>
/// Readiness check: returns <c>Unhealthy</c> when any tenant's <c>TenantDbContext</c> has
/// pending EF Core migrations, or when the per-tenant migration probe throws. Wired into
/// <c>/health/ready</c>, which Kubernetes / load-balancer readiness probes key off — so
/// a pod whose schema is behind the running build is kept out of rotation until the
/// standalone <c>FSH.Starter.DbMigrator</c> catches it up.
/// </summary>
public sealed class TenantMigrationsHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TenantMigrationsHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenants = await tenantStore.GetAllAsync().ConfigureAwait(false);

        var details = new Dictionary<string, object>();
        var tenantsWithPending = new List<string>();
        var tenantsWithError = new List<string>();

        foreach (var tenant in tenants)
        {
            try
            {
                using IServiceScope tenantScope = scope.ServiceProvider.CreateScope();

                tenantScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

                var dbContext = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();

                var pendingMigrations = (await dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false))
                    .ToArray();

                bool hasPending = pendingMigrations.Length > 0;
                if (hasPending)
                {
                    tenantsWithPending.Add(tenant.Id!);
                }

                details[tenant.Id!] = new
                {
                    tenant.Name,
                    tenant.IsActive,
                    tenant.ValidUpto,
                    HasPendingMigrations = hasPending,
                    PendingMigrations = pendingMigrations
                };
            }
            // Health checks must report errors, not throw — capture per-tenant failures as
            // detail entries so the readiness payload tells the operator which tenant is broken.
            catch (Exception ex)
            {
                tenantsWithError.Add(tenant.Id!);
                details[tenant.Id!] = new
                {
                    tenant.Name,
                    tenant.IsActive,
                    tenant.ValidUpto,
                    Error = ex.Message
                };
            }
        }

        if (tenantsWithError.Count > 0 || tenantsWithPending.Count > 0)
        {
            var description = BuildUnhealthyDescription(tenantsWithPending, tenantsWithError);
            return HealthCheckResult.Unhealthy(description, data: details);
        }

        return HealthCheckResult.Healthy("All tenants are at the head migration.", details);
    }

    private static string BuildUnhealthyDescription(List<string> pending, List<string> errored)
    {
        var parts = new List<string>(2);
        if (pending.Count > 0)
        {
            parts.Add($"pending migrations for tenant(s): {string.Join(", ", pending)}");
        }
        if (errored.Count > 0)
        {
            parts.Add($"error probing tenant(s): {string.Join(", ", errored)}");
        }
        return "Tenant schema is not at head — " + string.Join("; ", parts) +
               ". Run FSH.Starter.DbMigrator to apply pending migrations.";
    }
}
