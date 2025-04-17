using FSH.Framework.Auditing.Contracts.Dtos;

namespace FSH.Framework.Auditing.Services;
public interface IAuditService
{
    Task<IReadOnlyList<TrailDto>> GetUserTrailsAsync(Guid userId);
}