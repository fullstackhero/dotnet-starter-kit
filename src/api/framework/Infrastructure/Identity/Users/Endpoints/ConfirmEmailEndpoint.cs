using FSH.Framework.Core.Identity.Users.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
public static class ConfirmEmailEndpoint
{
    internal static RouteHandlerBuilder MapConfirmEmailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/confirm-email", (string userId, string code, string tenant, IUserService service) =>
        {
            return service.ConfirmEmailAsync(userId, code, tenant, default);
        })
        .WithName(nameof(ConfirmEmailEndpoint))
        .WithSummary("confirm user email")
        .WithDescription("confirm user email")
        .AllowAnonymous();
    }
}
