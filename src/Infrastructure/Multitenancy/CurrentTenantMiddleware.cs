using System.Net;
using DN.WebApi.Application.Identity.Exceptions;
using DN.WebApi.Application.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class CurrentTenantMiddleware : IMiddleware
{
    private readonly IStringLocalizer<CurrentTenantMiddleware> _localizer;
    private readonly ITenantService _tenantService;

    public CurrentTenantMiddleware(IStringLocalizer<CurrentTenantMiddleware> localizer, ITenantService tenantService)
    {
        _localizer = localizer;
        _tenantService = tenantService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!ExcludePath(context))
        {
            string? tenantId = TenantResolver.Resolver(context);
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