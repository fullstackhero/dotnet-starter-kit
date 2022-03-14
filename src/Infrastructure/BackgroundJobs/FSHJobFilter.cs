using System.Security.Claims;
using FSH.WebApi.Infrastructure.Common;
using FSH.WebApi.Infrastructure.Multitenancy;
using FSH.WebApi.Shared.Multitenancy;
using Hangfire.Client;
using Hangfire.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.BackgroundJobs;

public class FSHJobFilter : IClientFilter
{
    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

    private readonly IServiceProvider _services;

    public FSHJobFilter(IServiceProvider services) => _services = services;

    public void OnCreating(CreatingContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        Logger.InfoFormat("Set TenantId and UserId parameters to job {0}.{1}...", context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);

        using var scope = _services.CreateScope();

        var currentTenant = scope.ServiceProvider.GetService<FSHTenantInfo>();
        if (currentTenant?.Id is not null)
        {
            context.SetJobParameter(MultitenancyConstants.TenantIdName, currentTenant.Id);
        }

        var currentUser = scope.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext?.User;
        if (currentUser?.GetUserId() is { } userId)
        {
            context.SetJobParameter(QueryStringKeys.UserId, userId);
        }
    }

    public void OnCreated(CreatedContext context) =>
        Logger.InfoFormat(
            "Job created with parameters {0}",
            context.Parameters.Select(x => x.Key + "=" + x.Value).Aggregate((s1, s2) => s1 + ";" + s2));
}