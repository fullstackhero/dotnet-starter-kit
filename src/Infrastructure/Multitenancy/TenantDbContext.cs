using Finbuckle.MultiTenant.Stores;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public class TenantDbContext : EFCoreStoreDbContext<FSHTenantInfo>
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }
}