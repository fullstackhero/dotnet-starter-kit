using Microsoft.AspNetCore.Builder;

namespace FSH.Framework.Web.Security;

public static class SecurityExtensions
{
    public static IApplicationBuilder UseHeroSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}