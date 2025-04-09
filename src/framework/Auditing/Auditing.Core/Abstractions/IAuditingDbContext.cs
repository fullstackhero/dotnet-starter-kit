using FSH.Framework.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Core.Abstractions;
public interface IAuditingDbContext
{
    DbSet<Trail> Trails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
