using FSH.Framework.Auditing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Data;
public class AuditingDbContext : DbContext, IAuditingDbContext
{
    public DbSet<Trail> Trails { get; set; }

    public AuditingDbContext(DbContextOptions<AuditingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditingDbContext).Assembly);
    }

}
