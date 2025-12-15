using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Domain;
using FSH.Modules.Multitenancy.Provisioning;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Multitenancy.Data;

public class TenantDbContext : EFCoreStoreDbContext<AppTenantInfo>
{
    public const string Schema = "tenant";

    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantProvisioning> TenantProvisionings => Set<TenantProvisioning>();

    public DbSet<TenantProvisioningStep> TenantProvisioningSteps => Set<TenantProvisioningStep>();

    public DbSet<TenantTheme> TenantThemes => Set<TenantTheme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);
    }
}
