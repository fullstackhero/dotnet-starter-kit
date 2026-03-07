using System.Linq.Expressions;
using FSH.Framework.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Tests.Shared.Infrastructure;

public sealed class TestJobService : IJobService
{
    private readonly IServiceProvider _serviceProvider;

    public TestJobService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Enqueue(Expression<Action> methodCall) => Execute(methodCall);
    public string Enqueue(string queue, Expression<Func<Task>> methodCall) => Execute(methodCall);
    public string Enqueue(Expression<Func<Task>> methodCall) => Execute(methodCall);
    public string Enqueue<T>(Expression<Action<T>> methodCall) => Execute(methodCall);
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) => ExecuteAsync(methodCall).GetAwaiter().GetResult();

    public bool Requeue(string jobId) => throw new NotImplementedException();
    public bool Requeue(string jobId, string fromState) => throw new NotImplementedException();
    
    public string Schedule(Expression<Action> methodCall, TimeSpan delay) => Execute(methodCall);
    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay) => Execute(methodCall);
    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt) => Execute(methodCall);
    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt) => Execute(methodCall);
    
    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) => Execute(methodCall);
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) => ExecuteAsync(methodCall).GetAwaiter().GetResult();
    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt) => Execute(methodCall);
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt) => ExecuteAsync(methodCall).GetAwaiter().GetResult();

    public bool Delete(string jobId) => throw new NotImplementedException();
    public bool Delete(string jobId, string fromState) => throw new NotImplementedException();

    private static string Execute(Expression<Action> methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        methodCall.Compile().Invoke();
        return "inline";
    }

    private static string Execute(Expression<Func<Task>> methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        methodCall.Compile().Invoke().GetAwaiter().GetResult();
        return "inline";
    }

    private string Execute<T>(Expression<Action<T>> methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        using var scope = _serviceProvider.CreateScope();
#pragma warning disable CS8714
        var handler = scope.ServiceProvider.GetRequiredService<T>();
#pragma warning restore CS8714
        methodCall.Compile().Invoke(handler);
        return "inline";
    }

    private async Task<string> ExecuteAsync<T>(Expression<Func<T, Task>> methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        using var scope = _serviceProvider.CreateScope();
#pragma warning disable CS8714
        var handler = scope.ServiceProvider.GetRequiredService<T>();
#pragma warning restore CS8714
        await methodCall.Compile().Invoke(handler);
        return "inline";
    }
}
