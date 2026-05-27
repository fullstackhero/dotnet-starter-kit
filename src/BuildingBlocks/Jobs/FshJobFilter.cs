using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Common;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using Hangfire.Client;
using Hangfire.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Jobs;

public class FshJobFilter : IClientFilter
{
    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

    private readonly IServiceProvider _services;

    public FshJobFilter(IServiceProvider services) => _services = services;

    public void OnCreating(CreatingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Logger.InfoFormat("Set TenantId and UserId parameters to job {0}.{1}...",
            context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);

        using var scope = _services.CreateScope();

        var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;

        if (httpContext is null)
        {
            // No HTTP context (e.g. recurring/background job creation) – skip setting tenant/user.
            Logger.WarnFormat("No HttpContext available for job {0}.{1}; skipping tenant/user parameters.",
                context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);
            return;
        }

        var mtAccessor = scope.ServiceProvider.GetService<IMultiTenantContextAccessor>();
        var tenantInfo = mtAccessor?.MultiTenantContext?.TenantInfo;
        if (tenantInfo is not null)
        {
            context.SetJobParameter(MultitenancyConstants.Identifier, tenantInfo);
        }

        var userId = httpContext.User.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            context.SetJobParameter(QueryStringKeys.UserId, userId);
        }
    }

    public void OnCreated(CreatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Logger.InfoFormat(
            "Job created with parameters {0}",
            context.Parameters.Select(x => x.Key + "=" + x.Value).Aggregate((s1, s2) => s1 + ";" + s2));
    }
}