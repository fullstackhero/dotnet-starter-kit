using FSH.Framework.Auditing.Core.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Core.Abstractions;
public interface IAuditTrailDbContext
{
    DbSet<AuditTrail> AuditTrails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
