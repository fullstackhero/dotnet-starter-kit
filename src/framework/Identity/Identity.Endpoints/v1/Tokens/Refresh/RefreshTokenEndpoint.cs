using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Refresh;
public static class RefreshTokenEndpoint
{
    internal static RouteHandlerBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/refresh", (TokenRefreshRequest request,
            string tenant,
            ITokenService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            string ip = context.GetIpAddress();
            return service.RefreshTokenAsync(request, ip!, cancellationToken);
        })
        .WithName(nameof(RefreshTokenEndpoint))
        .WithSummary("refresh JWTs")
        .WithDescription("refresh JWTs")
        .AllowAnonymous();
    }
}
