using FSH.Framework.Auditing.Abstractions;
using FSH.Framework.Auditing.Models;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Services;
public class AuditService(IAuditTrailDbContext context) : IAuditService
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
