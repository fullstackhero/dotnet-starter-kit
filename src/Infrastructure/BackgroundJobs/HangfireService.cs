using System.Linq.Expressions;
using System.Text.Json;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Infrastructure.Multitenancy;
using Hangfire;
using MediatR;

namespace FSH.WebApi.Infrastructure.BackgroundJobs;

public class HangfireService : IJobService
{
    private readonly FSHTenantInfo _currentTenant;
    private readonly ICurrentUser _currentUser;

    public HangfireService(FSHTenantInfo currentTenant, ICurrentUser currentUser) =>
        (_currentTenant, _currentUser) = (currentTenant, currentUser);

    public string Enqueue(IRequest request) =>
        BackgroundJob.Enqueue<HangfireMediatorBridge>(bridge => bridge.Send(GetDisplayName(request), request, default));
    public void AddOrUpdate(string recurringJobId, IRequest request, string cronExpression, TimeZoneInfo? timeZone = null, string queue = "default") =>
        RecurringJob.AddOrUpdate<HangfireMediatorBridge>(
            recurringJobId,
            bridge => bridge.Send(GetDisplayName(request), _currentTenant.Id, _currentUser.GetUserId().ToString(), request, default),
            cronExpression,
            timeZone,
            queue);

    private static string GetDisplayName(IRequest request) => $"{request.GetType().Name} {JsonSerializer.Serialize(request, request.GetType())}";

    public bool Delete(string jobId) =>
        BackgroundJob.Delete(jobId);

    public bool Delete(string jobId, string fromState) =>
        BackgroundJob.Delete(jobId, fromState);

    public string Enqueue(Expression<Func<Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Action<T>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Enqueue(Expression<Action> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public bool Requeue(string jobId) =>
        BackgroundJob.Requeue(jobId);

    public bool Requeue(string jobId, string fromState) =>
        BackgroundJob.Requeue(jobId, fromState);

    public string Schedule(Expression<Action> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt) =>
        BackgroundJob.Schedule(methodCall, enqueueAt);

    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt) =>
        BackgroundJob.Schedule(methodCall, enqueueAt);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt) =>
        BackgroundJob.Schedule(methodCall, enqueueAt);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt) =>
        BackgroundJob.Schedule(methodCall, enqueueAt);
}