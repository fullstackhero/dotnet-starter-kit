using FSH.Framework.Auditing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Data;
public interface IAuditingDbContext
{
    DbSet<Trail> Trails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}