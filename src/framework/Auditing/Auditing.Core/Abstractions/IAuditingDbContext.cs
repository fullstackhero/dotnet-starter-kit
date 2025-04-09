using FSH.Framework.Auditing.Core.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Core.Abstractions;
public interface IAuditingDbContext
{
    DbSet<Trail> Trails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
