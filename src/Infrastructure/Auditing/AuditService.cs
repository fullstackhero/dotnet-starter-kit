using DN.WebApi.Application.Auditing;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Auditing;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context) => _context = context;

    public async Task<List<AuditDto>> GetUserTrailsAsync(Guid userId)
    {
        var trails = await _context.AuditTrails
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Id)
            .Take(250)
            .ToListAsync();

        return trails.Adapt<List<AuditDto>>();
    }
}