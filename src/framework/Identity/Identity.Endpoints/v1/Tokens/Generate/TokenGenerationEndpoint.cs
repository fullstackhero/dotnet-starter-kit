using FSH.Framework.Identity.Core.Dtos;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens.Generate;
public static class TokenGenerationEndpoint
{
    internal static RouteHandlerBuilder MapTokenGenerationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (TokenGenerationRequest request,
            string tenant,
            ITokenService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            string ip = context.GetIpAddress();
            return service.GenerateTokenAsync(request, ip!, cancellationToken);
        })
        .WithName(nameof(TokenGenerationEndpoint))
        .WithSummary("generate JWTs")
        .WithDescription("generate JWTs")
        .AllowAnonymous();
    }
}
