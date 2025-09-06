using FSH.Framework.Auditing.Contracts.v1.GetUserTrails;
using FSH.Framework.Auditing.Services;
using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Auditing.Features.v1.GetUserTrails;
internal sealed class GetUserTrailsQueryHandler(IAuditService auditService)
        : IQueryHandler<GetUserTrailsQuery, GetUserTrailsQueryResponse>
{
    public async Task<GetUserTrailsQueryResponse> HandleAsync(GetUserTrailsQuery query, CancellationToken cancellationToken = default)
    {
        var trails = await auditService.GetUserTrailsAsync(query.UserId);
        return new GetUserTrailsQueryResponse(trails);
    }
}