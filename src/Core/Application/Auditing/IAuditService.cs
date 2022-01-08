using DN.WebApi.Application.Common.Interfaces;

namespace DN.WebApi.Application.Auditing;

public interface IAuditService : ITransientService
{
    Task<List<AuditDto>> GetUserTrailsAsync(Guid userId);
}