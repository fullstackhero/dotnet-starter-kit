using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
public sealed class GetUserTrailsHandler(IAuditService auditService)
    : IQueryHandler<GetUserTrailsQuery, GetUserTrailsResponse>
{
    public async Task<GetUserTrailsResponse> HandleAsync(GetUserTrailsQuery query, CancellationToken cancellationToken = default)
    {
        var trails = await auditService.GetUserTrailsAsync(query.UserId);
        return new GetUserTrailsResponse(trails);
    }
}
