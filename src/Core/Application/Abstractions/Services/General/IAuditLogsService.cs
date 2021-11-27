using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.AuditLogs;

namespace DN.WebApi.Application.Abstractions.Services.General;

public interface IAuditLogsService : ITransientService
{
    Task<IResult<IEnumerable<AuditResponse>>> GetUserTrailsAsync(Guid userId);
}