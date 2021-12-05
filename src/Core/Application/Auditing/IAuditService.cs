using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Auditing;

namespace DN.WebApi.Application.Auditing;

public interface IAuditService : ITransientService
{
    Task<IResult<IEnumerable<AuditResponse>>> GetUserTrailsAsync(Guid userId);
}