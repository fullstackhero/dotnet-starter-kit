using System.Linq.Expressions;

namespace FSH.WebApi.Application.Common.Interfaces;

public interface IJobService : ITransientService
{
    string Enqueue(IRequest request);
    void AddOrUpdate(string recurringJobId, IRequest request, string cronExpression, TimeZoneInfo? timeZone = null, string queue = "default");

    string Enqueue(Expression<Action> methodCall);
    string Enqueue(Expression<Func<Task>> methodCall);
    string Enqueue<T>(Expression<Action<T>> methodCall);
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    string Schedule(Expression<Action> methodCall, TimeSpan delay);
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
    string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt);
    string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt);
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
    string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt);
    string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt);

    bool Delete(string jobId);
    bool Delete(string jobId, string fromState);

    bool Requeue(string jobId);
    bool Requeue(string jobId, string fromState);
}