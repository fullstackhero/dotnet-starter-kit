using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Tenant.Data;
public class TenantDbContext : EFCoreStoreDbContext<FshTenantInfo>
{
    public const string Schema = "tenant";
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FshTenantInfo>().ToTable("Tenants", Schema);
    }
}