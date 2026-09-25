using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// TestServer has no real socket, so <c>Connection.RemoteIpAddress</c> is null and the
/// forwarded-headers trust check (known proxies/networks) can't be exercised. This filter runs
/// before the app pipeline (hence before UseForwardedHeaders) and stamps the connection IP from the
/// <c>X-Test-Remote-Ip</c> header so a test can present itself as a trusted or untrusted upstream.
/// Inert for requests that don't carry the header.
/// </summary>
public sealed class TestRemoteIpStartupFilter : IStartupFilter
{
    public const string RemoteIpHeader = "X-Test-Remote-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                var header = context.Request.Headers[RemoteIpHeader].FirstOrDefault();
                if (!string.IsNullOrEmpty(header) && IPAddress.TryParse(header, out var ip))
                {
                    context.Connection.RemoteIpAddress = ip;
                }

                await nextMiddleware();
            });

            next(app);
        };
}
