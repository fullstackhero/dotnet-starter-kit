using FSH.Framework.Auditing.Contracts.Dtos;
using FSH.Framework.Auditing.Data;
using FSH.Modules.Auditing.Core.Mappings;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Auditing.Services;
public class AuditService(IAuditingDbContext context) : IAuditService
{
    public async Task<IReadOnlyList<TrailDto>> GetUserTrailsAsync(Guid userId)
    {
        var trails = await context.Trails
           .Where(a => a.UserId == userId)
           .OrderByDescending(a => a.DateTime)
           .Take(250)
           .ToListAsync();

        return trails.ToDtoList();
    }
}