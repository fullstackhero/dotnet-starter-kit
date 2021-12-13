using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Extensions;
using DN.WebApi.Infrastructure.Multitenancy;
using Hangfire.Client;
using Hangfire.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Hangfire;

public class TenantJobFilter : IClientFilter
{
    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
    private readonly IServiceProvider _services;

    public TenantJobFilter(IServiceProvider services)
    {
        _services = services;
    }

    public void OnCreating(CreatingContext context)
    {
        // throw exception if context is null
        ArgumentNullException.ThrowIfNull(context);

        Logger.InfoFormat("Set TenantId and UserId parameters to the job {0}.{1}...", context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);

        using var scope = _services.CreateScope();
        var httpContext = scope.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext is null) throw new InvalidOperationException("Can't to create a TenantJob without HttpContext.");

        string? tenantId = TenantResolver.Resolver(httpContext);
        string? userId = httpContext.User.GetUserId();
        context.SetJobParameter(QueryConstants.Tenant, tenantId);
        context.SetJobParameter(QueryConstants.UserId, userId);
    }

    public void OnCreated(CreatedContext context)
    {
        Logger.InfoFormat(
            "Job created with parameters {0}",
            context.Parameters.Select(x => x.Key + "=" + x.Value).Aggregate((s1, s2) => s1 + ";" + s2));
    }
}