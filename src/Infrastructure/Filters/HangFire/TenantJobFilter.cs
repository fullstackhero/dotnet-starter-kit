using System;
using System.Linq;
using DN.WebApi.Application.Abstractions.Services.Identity;
using Hangfire.Client;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace DN.WebApi.Infrastructure.Filters.HangFire
{
    public class TenantJobFilter : IClientFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly ICurrentUser _currentUser;

        public TenantJobFilter(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public void OnCreating(CreatingContext context)
        {
            Logger.InfoFormat("Set TenantId and UserId parameters to the job {0}.{1}...", context.Job.Method.ReflectedType.FullName, context.Job.Method.Name);

            if (context == null) throw new ArgumentNullException(nameof(context));

            context.SetJobParameter("tenant", _currentUser.GetTenant());
            context.SetJobParameter("userId", _currentUser.GetUserId());
        }

        public void OnCreated(CreatedContext context)
        {
            Logger.InfoFormat(
                "Job created with parameters {0}",
                context.Parameters.Select(x => x.Key + "=" + x.Value).Aggregate((s1, s2) => s1 + ";" + s2));
        }
    }
}