using FSH.Framework.Core.Identity.Tokens;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Tokens.Endpoints;
public static class TokenGenerationEndpoint
{
    internal static RouteHandlerBuilder MapTokenGenerationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (TokenGenerationCommand request,
            ITokenService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            string ip = "N/A";
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ipList))
            {
                ip = ipList.FirstOrDefault() ?? "N/A";
            }
            else if (context.Connection.RemoteIpAddress != null)
            {
                ip = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
            return service.GenerateTokenAsync(request, ip!, cancellationToken);
        })
        .WithName(nameof(TokenGenerationEndpoint))
        .WithSummary("generate JWTs")
        .WithDescription("generate JWTs").AllowAnonymous();
    }
}
