using FSH.Framework.Core.Audit;
using FSH.Framework.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Audit.Endpoints;

public static class GetUserAuditTrailEndpoint
{
    internal static RouteHandlerBuilder MapGetUserAuditTrailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}/audit-trails", (Guid id, IAuditService service) =>
        {
            return service.GetUserTrailsAsync(id);
        })
        .WithName(nameof(GetUserAuditTrailEndpoint))
        .WithSummary("Get user's audit trail details")
        .RequirePermission("Permissions.AuditTrails.View")
        .WithDescription("Get user's audit trail details.");
    }
}
