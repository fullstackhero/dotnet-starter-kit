using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.SecurityHeaders;

public static class Extensions
{
    internal static IServiceCollection ConfigureSecurityHeaders(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SecurityHeaderOptions>(config.GetSection(nameof(SecurityHeaderOptions)));

        return services;
    }
    
    internal static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<SecurityHeaderOptions>>().Value;
        
        if (options.Enable)
        {
            app.Use(async (context, next) =>
            {
                if (!context.Response.HasStarted)
                {
                    if (!string.IsNullOrWhiteSpace(options.Headers.XFrameOptions))
                    {
                        context.Response.Headers.XFrameOptions = options.Headers.XFrameOptions;
                    }

                    if (!string.IsNullOrWhiteSpace(options.Headers.XContentTypeOptions))
                    {
                        context.Response.Headers.XContentTypeOptions = options.Headers.XContentTypeOptions;
                    }

                    if (!string.IsNullOrWhiteSpace(options.Headers.ReferrerPolicy))
                    {
                        context.Response.Headers.Referer = options.Headers.ReferrerPolicy;
                    }

                    if (!string.IsNullOrWhiteSpace(options.Headers.PermissionsPolicy))
                    {
                        context.Response.Headers["Permissions-Policy"] = options.Headers.PermissionsPolicy;
                    }

                    if (!string.IsNullOrWhiteSpace(options.Headers.XXSSProtection))
                    {
                        context.Response.Headers.XXSSProtection = options.Headers.XXSSProtection;
                    }
                
                    if (!string.IsNullOrWhiteSpace(options.Headers.ContentSecurityPolicy))
                    {
                        context.Response.Headers.ContentSecurityPolicy = options.Headers.ContentSecurityPolicy;
                    }
                
                    if (!string.IsNullOrWhiteSpace(options.Headers.StrictTransportSecurity))
                    {
                        context.Response.Headers.StrictTransportSecurity = options.Headers.StrictTransportSecurity;
                    }
                }
            
                await next.Invoke();
            });
        }
        
        return app;
    }
}
