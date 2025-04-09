using FSH.Framework.Auditing.Contracts;

namespace FSH.Framework.Auditing.Core.Abstractions;
public interface IAuditService
{
    Task<List<Trail>> GetUserTrailsAsync(Guid userId);
}
