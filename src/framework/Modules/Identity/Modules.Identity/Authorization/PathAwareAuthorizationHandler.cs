using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace FSH.Framework.Identity.Authorization;
public class PathAwareAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var path = context.Request.Path;
        var allowedPaths = new[]
        {
            new PathString("/scalar"),
            new PathString("/openapi"),
            new PathString("/favicon.ico")
        };
        if (allowedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            // ✅ Respect routing + continue the pipeline
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                await next(context);
                return;
            }

            // If no endpoint is found, return 404 explicitly
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Endpoint not found.");
            return;
        }

        await _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}