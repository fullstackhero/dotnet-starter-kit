using FSH.Framework.Auditing.Core.Dtos;

namespace FSH.Framework.Auditing.Core.Abstractions;
public interface IAuditService
{
    Task<List<AuditTrail>> GetUserTrailsAsync(Guid userId);
}
