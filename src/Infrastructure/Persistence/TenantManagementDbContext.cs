using DN.WebApi.Domain.Configuration;
using DN.WebApi.Domain.Entities.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence
{
    public class TenantManagementDbContext : DbContext
    {
        public TenantManagementDbContext(DbContextOptions<TenantManagementDbContext> options)
        : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TenantTypeConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}