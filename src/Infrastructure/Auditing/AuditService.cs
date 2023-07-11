using FSH.WebApi.Application.Auditing;
using FSH.WebApi.Infrastructure.Persistence.Context;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Auditing;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context) => _context = context;

    public async Task<List<AuditDto>> GetUserTrailsAsync(Guid userId)
    {
        return await _context.AuditTrails
            .Where(a => a.UserId == userId)
            .Select(at => new AuditDto
            {
                Id = at.Id,
                UserId = at.UserId,
                Type = at.Type,
                TableName = at.TableName,
                DateTime = at.DateTime,
                OldValues = at.OldValues,
                NewValues = at.NewValues,
                AffectedColumns = at.AffectedColumns,
                PrimaryKey = at.PrimaryKey
            })
            .OrderByDescending(a => a.DateTime)
            .Take(250)
            .ToListAsync();
    }
}