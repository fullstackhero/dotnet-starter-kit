using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Auditing.Core.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Infrastructure.Services;
public class AuditService(IAuditingDbContext context) : IAuditService
{
    public async Task<List<AuditTrail>> GetUserTrailsAsync(Guid userId)
    {
        var trails = await context.AuditTrails
           .Where(a => a.UserId == userId)
           .OrderByDescending(a => a.DateTime)
           .Take(250)
           .ToListAsync();
        return trails;
    }
}
