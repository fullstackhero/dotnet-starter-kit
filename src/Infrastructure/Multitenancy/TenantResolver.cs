using DN.WebApi.Infrastructure.Identity.Extensions;
using Microsoft.AspNetCore.Http;

namespace DN.WebApi.Infrastructure.Multitenancy;

public static class TenantResolver
{
    public static string Resolver(HttpContext context)
    {
        string tenantId = ResolveFromUserAuth(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        tenantId = ResolveFromHeader(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        tenantId = ResolveFromQuery(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        return default;
    }

    private static string ResolveFromUserAuth(HttpContext context)
    {
        return context.User.GetTenant();
    }

    private static string ResolveFromHeader(HttpContext context)
    {
        context.Request.Headers.TryGetValue("tenant", out var tenantFromHeader);
        return tenantFromHeader;
    }

    private static string ResolveFromQuery(HttpContext context)
    {
        context.Request.Query.TryGetValue("tenant", out var tenantFromQueryString);
        return tenantFromQueryString;
    }
}