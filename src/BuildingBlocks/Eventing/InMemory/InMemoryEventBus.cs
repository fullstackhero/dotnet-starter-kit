using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

namespace FSH.Framework.Eventing.InMemory;

/// <summary>
/// In-memory event bus implementation used for single-process deployments.
/// It resolves handlers from DI and optionally uses an inbox store for idempotency.
/// </summary>
public sealed partial class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly IEventTenantScope _tenantScope;

    // The closed handler interface type and its HandleAsync method are stable per event type, so
    // they are resolved once and cached rather than recomputing reflection on every published event.
    private static readonly ConcurrentDictionary<Type, HandlerDispatch> DispatchCache = new();

    private readonly record struct HandlerDispatch(Type HandlerInterfaceType, MethodInfo HandleMethod);

    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger, IEventTenantScope tenantScope)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _tenantScope = tenantScope;
    }

    private static HandlerDispatch GetDispatch(Type eventType)
        => DispatchCache.GetOrAdd(eventType, static et =>
        {
            var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(et);
            var method = handlerInterfaceType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
                ?? throw new InvalidOperationException($"IIntegrationEventHandler<{et.Name}> does not declare HandleAsync.");
            return new HandlerDispatch(handlerInterfaceType, method);
        });

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
        => PublishAsync(new[] { @event }, ct);

    public async Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            await PublishSingleAsync(@event, ct).ConfigureAwait(false);
        }
    }

    private async Task PublishSingleAsync(IIntegrationEvent @event, CancellationToken ct)
    {
        var eventType = @event.GetType();
        LogPublishingEvent(eventType.FullName, @event.Id);

        var dispatch = GetDispatch(eventType);

        // Set tenant context BEFORE resolving handlers — MultiTenantDbContext captures TenantInfo at
        // construction, so a late tenant NREs the query filter. This is what makes background publishers work.
        using (_tenantScope.Begin(@event.TenantId))
        {
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;

            var handlers = ResolveHandlers(provider, dispatch.HandlerInterfaceType);
            if (handlers.Length == 0)
            {
                LogNoHandlers(eventType.FullName);
                return;
            }

            var inbox = provider.GetService<IInboxStore>();

            foreach (var handler in handlers)
            {
                await InvokeHandlerAsync(handler, dispatch.HandleMethod, eventType, @event, inbox, ct).ConfigureAwait(false);
            }
        }
    }

    private static object[] ResolveHandlers(IServiceProvider provider, Type handlerInterfaceType)
        => provider.GetServices(handlerInterfaceType).Where(h => h is not null).ToArray()!;

    private async Task InvokeHandlerAsync(
        object handler,
        MethodInfo handleMethod,
        Type eventType,
        IIntegrationEvent @event,
        IInboxStore? inbox,
        CancellationToken ct)
    {
        var handlerName = handler.GetType().FullName ?? handler.GetType().Name;

        if (await ShouldSkipProcessedEventAsync(inbox, @event.Id, handlerName, ct).ConfigureAwait(false))
        {
            LogSkippingProcessed(@event.Id, handlerName);
            return;
        }

        await ExecuteHandlerAsync(handler, handleMethod, @event, eventType, handlerName, inbox, ct).ConfigureAwait(false);
    }

    private static async Task<bool> ShouldSkipProcessedEventAsync(IInboxStore? inbox, Guid eventId, string handlerName, CancellationToken ct)
    {
        return inbox != null && await inbox.HasProcessedAsync(eventId, handlerName, ct).ConfigureAwait(false);
    }

    private async Task ExecuteHandlerAsync(
        object handler,
        MethodInfo method,
        IIntegrationEvent @event,
        Type eventType,
        string handlerName,
        IInboxStore? inbox,
        CancellationToken ct)
    {
        try
        {
            var task = (Task)method.Invoke(handler, new object[] { @event, ct })!;
            await task.ConfigureAwait(false);

            if (inbox != null)
            {
                await inbox.MarkProcessedAsync(@event.Id, handlerName, @event.TenantId, eventType.AssemblyQualifiedName ?? eventType.FullName!, ct)
                    .ConfigureAwait(false);
            }
        }
        // Broad catch is intentional: log and re-throw to ensure all handler
        // failures are captured regardless of exception type.
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling integration event {EventId} with handler {Handler}", @event.Id, handlerName);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Publishing integration event {EventType} ({EventId})")]
    private partial void LogPublishingEvent(string? eventType, Guid eventId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No handlers registered for integration event type {EventType}")]
    private partial void LogNoHandlers(string? eventType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping already processed integration event {EventId} for handler {Handler}")]
    private partial void LogSkippingProcessed(Guid eventId, string handler);
}