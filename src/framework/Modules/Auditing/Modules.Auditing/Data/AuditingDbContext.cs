using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Auditing.Core.Entities;
using FSH.Framework.Core.Messaging.Events;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Auditing.Data;
public class AuditingDbContext : FshDbContext, IAuditingDbContext
{
    public DbSet<Trail> Trails { get; set; }

    public AuditingDbContext(
        IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor,
        DbContextOptions<AuditingDbContext> options,
        IEventPublisher publisher,
        IOptions<DatabaseOptions> settings) : base(multiTenantContextAccessor, options, publisher, settings) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditingDbContext).Assembly);
    }

}