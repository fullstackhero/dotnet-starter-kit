using DN.WebApi.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence
{
    public class TenantDbContext : DbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}