using FSH.Framework.Auditing.Contracts;
using FSH.Framework.Auditing.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Infrastructure.Data;
public class AuditingDbContext : DbContext, IAuditingDbContext
{
    public DbSet<Trail> Trails { get; set; }

    public AuditingDbContext(DbContextOptions<AuditingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditingDbContext).Assembly);
    }
}
