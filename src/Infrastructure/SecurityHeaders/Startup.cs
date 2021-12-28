using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace DN.WebApi.Infrastructure.SecurityHeaders;

internal static class Startup
{
    internal static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IConfiguration config)
    {
        var settings = config.GetSection(nameof(SecurityHeaderSettings)).Get<SecurityHeaderSettings>();

        if (settings != null && settings.Enable)
        {
            return app.Use(async (context, next) =>
            {
                if (!string.IsNullOrEmpty(settings.XFrameOptions) && !string.IsNullOrWhiteSpace(settings.XFrameOptions))
                {
                    context.Response.Headers.Add(Consts.XFRAMEOPTIONS, settings.XFrameOptions);
                }

                if (!string.IsNullOrEmpty(settings.XContentTypeOptions) && !string.IsNullOrWhiteSpace(settings.XContentTypeOptions))
                {
                    context.Response.Headers.Add(Consts.XCONTENTTYPEOPTIONS, settings.XContentTypeOptions);
                }

                if (!string.IsNullOrEmpty(settings.ReferrerPolicy) && !string.IsNullOrWhiteSpace(settings.ReferrerPolicy))
                {
                    context.Response.Headers.Add(Consts.REFERRERPOLICY, settings.ReferrerPolicy);
                }

                if (!string.IsNullOrEmpty(settings.PermissionsPolicy) && !string.IsNullOrWhiteSpace(settings.PermissionsPolicy))
                {
                    context.Response.Headers.Add(Consts.PERMISSIONSPOLICY, settings.PermissionsPolicy);
                }

                if (!string.IsNullOrEmpty(settings.SameSite) && !string.IsNullOrWhiteSpace(settings.SameSite))
                {
                    context.Response.Headers.Add(Consts.SAMESITE, settings.SameSite);
                }

                if (!string.IsNullOrEmpty(settings.XXSSProtection) && !string.IsNullOrWhiteSpace(settings.XXSSProtection))
                {
                    context.Response.Headers.Add(Consts.XXSSPROTECTION, settings.XXSSProtection);
                }

                await next();
            });

        }

        return app;
    }
}