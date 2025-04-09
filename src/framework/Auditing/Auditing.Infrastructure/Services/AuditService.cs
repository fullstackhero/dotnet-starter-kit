using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Auditing.Core.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Infrastructure.Services;
public class AuditService(IAuditingDbContext context) : IAuditService
{
    public async Task<List<Trail>> GetUserTrailsAsync(Guid userId)
    {
        var trails = await context.Trails
           .Where(a => a.UserId == userId)
           .OrderByDescending(a => a.DateTime)
           .Take(250)
           .ToListAsync();
        return trails;
    }
}
