using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Tokens.Endpoints;
internal static class Extensions
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRefreshTokenEndpoint();
        app.MapTokenGenerationEndpoint();
        return app;
    }

    public static string GetIpAddress(this HttpContext context)
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
        return ip;

    }
}
