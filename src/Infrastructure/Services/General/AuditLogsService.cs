using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Shared.DTOs.AuditLogs;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Services.General;

public class AuditLogsService : IAuditLogsService
{
    private readonly ApplicationDbContext _context;

    public AuditLogsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IResult<IEnumerable<AuditResponse>>> GetUserTrailsAsync(Guid userId)
    {
        var trails = await _context.AuditTrails.Where(a => a.UserId == userId).OrderByDescending(a => a.Id).Take(250).ToListAsync();
        var mappedLogs = trails.Adapt<IEnumerable<AuditResponse>>();
        return await Result<IEnumerable<AuditResponse>>.SuccessAsync(mappedLogs);
    }
}