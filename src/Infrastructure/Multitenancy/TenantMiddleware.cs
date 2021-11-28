using DN.WebApi.Application.Identity.Exceptions;
using DN.WebApi.Application.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class TenantMiddleware : IMiddleware
{
    private readonly IStringLocalizer<TenantMiddleware> _localizer;
    private readonly ITenantService _tenantService;

    public TenantMiddleware(IStringLocalizer<TenantMiddleware> localizer, ITenantService tenantService)
    {
        _localizer = localizer;
        _tenantService = tenantService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!ExcludePath(context))
        {
            string tenantId = TenantResolver.Resolver(context);
            if (!string.IsNullOrEmpty(tenantId))
            {
                _tenantService.SetCurrentTenant(tenantId);
            }
            else
            {
                throw new IdentityException(_localizer["auth.failed"], statusCode: HttpStatusCode.Unauthorized);
            }
        }

        await next(context);
    }

    private bool ExcludePath(HttpContext context)
    {
        var listExclude = new List<string>()
            {
                "/swagger",
                "/jobs"
            };

        foreach (string item in listExclude)
        {
            if (context.Request.Path.StartsWithSegments(item))
            {
                return true;
            }
        }

        return false;
    }
}