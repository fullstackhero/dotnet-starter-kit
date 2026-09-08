using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using FSH.Framework.Persistence.Providers;
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

    public DbSet<TenantExpiryNotice> TenantExpiryNotices => Set<TenantExpiryNotice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);
    }

    /// <summary>
    /// This context does not derive from BaseDbContext, so it registers the framework's provider
    /// conventions itself — without them the portable column intent is never resolved.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.AddHeroProviderConventions(DbProviderResolver.FromEfProviderName(Database.ProviderName));
    }
}