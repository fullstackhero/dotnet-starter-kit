using System.Linq.Expressions;
using FSH.Framework.Jobs.Services;

namespace FSH.Starter.DbMigrator;

/// <summary>
/// Satisfies <see cref="IJobService"/> in the migrator's DI graph without
/// pulling in Hangfire's background-job server (which needs its own DB
/// schema and worker threads — overkill for a one-shot console).
///
/// All operations throw — the migrator's code paths
/// (<c>ITenantService.MigrateTenantAsync</c> / <c>SeedTenantAsync</c>)
/// never enqueue jobs. If a regression starts enqueuing during migration
/// the throw makes the misuse obvious.
/// </summary>
internal sealed class NoOpJobService : IJobService
{
    private static InvalidOperationException Reject(string method) =>
        new($"IJobService.{method} called from the DbMigrator — jobs are not supported in the one-shot migrator. " +
            "If this code path is now needed at migration time, enable Hangfire in DbMigrator's AddHeroPlatform options.");

    public bool Delete(string jobId) => throw Reject(nameof(Delete));
    public bool Delete(string jobId, string fromState) => throw Reject(nameof(Delete));
    public string Enqueue(Expression<Action> methodCall) => throw Reject(nameof(Enqueue));
    public string Enqueue(string queue, Expression<Func<Task>> methodCall) => throw Reject(nameof(Enqueue));
    public string Enqueue(Expression<Func<Task>> methodCall) => throw Reject(nameof(Enqueue));
    public string Enqueue<T>(Expression<Action<T>> methodCall) => throw Reject(nameof(Enqueue));
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) => throw Reject(nameof(Enqueue));
    public bool Requeue(string jobId) => throw Reject(nameof(Requeue));
    public bool Requeue(string jobId, string fromState) => throw Reject(nameof(Requeue));
    public string Schedule(Expression<Action> methodCall, TimeSpan delay) => throw Reject(nameof(Schedule));
    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay) => throw Reject(nameof(Schedule));
    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt) => throw Reject(nameof(Schedule));
    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt) => throw Reject(nameof(Schedule));
    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) => throw Reject(nameof(Schedule));
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) => throw Reject(nameof(Schedule));
    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt) => throw Reject(nameof(Schedule));
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt) => throw Reject(nameof(Schedule));
}
