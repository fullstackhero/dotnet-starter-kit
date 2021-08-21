using System.Linq.Expressions;
using DN.WebApi.Application.Abstractions.Services.General;
using Hangfire;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class HangfireService : IJobService
    {
        public string Enqueue(Expression<Func<Task>> methodCall)
        {
            return BackgroundJob.Enqueue(methodCall);
        }
    }
}