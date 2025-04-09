using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
public static class GetUserTrailsEndpoint
{
    public static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/{userId:guid}/trails", async (
            Guid userId,
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<GetUserTrailsQuery, GetUserTrailsResponse>(
                new GetUserTrailsQuery(userId), cancellationToken);

            return TypedResults.Ok(result);
        })
        .WithName("GetUserTrails")
        .WithSummary("Get user's audit trail details")
        .WithDescription("Returns the audit trail details for a specific user.")
        .RequirePermission("Permissions.AuditTrails.View");
    }
}

