using System;
using System.Linq;
using DN.WebApi.Infrastructure.Extensions;
using Hangfire.Client;
using Hangfire.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Filters.HangFire
{
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
            Logger.InfoFormat("Set TenantId and UserId parameters to the job {0}.{1}...", context.Job.Method.ReflectedType.FullName, context.Job.Method.Name);

            if (context == null) throw new ArgumentNullException(nameof(context));

            using var scope = _services.CreateScope();
            var contextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
            context.SetJobParameter("tenant", contextAccessor.HttpContext.User.GetTenant());
            context.SetJobParameter("userId", contextAccessor.HttpContext.User.GetUserId());
        }

        public void OnCreated(CreatedContext context)
        {
            Logger.InfoFormat(
                "Job created with parameters {0}",
                context.Parameters.Select(x => x.Key + "=" + x.Value).Aggregate((s1, s2) => s1 + ";" + s2));
        }
    }
}