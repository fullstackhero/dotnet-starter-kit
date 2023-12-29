using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Multitenancy;
public class TenantDbContext : EFCoreStoreDbContext<FshTenantInfo>
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FshTenantInfo>().ToTable("Tenants", SchemaNames.Tenant);
    }
}
