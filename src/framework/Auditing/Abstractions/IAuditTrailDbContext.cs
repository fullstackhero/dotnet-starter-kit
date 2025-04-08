using FSH.Framework.Auditing.Models;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Abstractions;
public interface IAuditTrailDbContext
{
    DbSet<AuditTrail> AuditTrails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
