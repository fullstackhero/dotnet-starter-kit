using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;

public static partial class GetUserTrails
{
    public sealed class Handler(IAuditService auditService)
    : IQueryHandler<Query, Response>
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            var trails = await auditService.GetUserTrailsAsync(query.UserId);
            return new Response(trails);
        }
    }
}

