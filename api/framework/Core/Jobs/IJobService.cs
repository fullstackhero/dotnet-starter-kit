using System.Linq.Expressions;

namespace FSH.Framework.Core.Jobs;

public interface IJobService
{
    bool Delete(string jobId);

    bool Delete(string jobId, string fromState);

    string Enqueue(Expression<Action> methodCall);

    string Enqueue(string queue, Expression<Func<Task>> methodCall);

    string Enqueue(Expression<Func<Task>> methodCall);

    string Enqueue<T>(Expression<Action<T>> methodCall);

    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    bool Requeue(string jobId);

    bool Requeue(string jobId, string fromState);

    string Schedule(Expression<Action> methodCall, TimeSpan delay);

    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);

    string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt);

    string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt);

    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);

    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt);

    string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt);
}
