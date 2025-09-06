using FSH.Framework.Auditing.Contracts.v1.GetUserTrails;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Auditing.Features.v1.GetUserTrails;
public static class GetUserTrailsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/{userId:guid}/trails", async (
            Guid userId,
            [FromServices] IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserTrailsQuery(userId);
            var result = await dispatcher.SendAsync(query, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetUserTrailsEndpoint))
        .WithSummary("Get user's audit trail details")
        .WithDescription("Returns the audit trail details for a specific user.")
        .RequirePermission("Permissions.AuditTrails.View");
    }
}