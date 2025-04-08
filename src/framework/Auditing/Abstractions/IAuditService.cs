using FSH.Framework.Auditing.Models;

namespace FSH.Framework.Auditing.Abstractions;
public interface IAuditService
{
    Task<List<AuditTrail>> GetUserTrailsAsync(Guid userId);
}
