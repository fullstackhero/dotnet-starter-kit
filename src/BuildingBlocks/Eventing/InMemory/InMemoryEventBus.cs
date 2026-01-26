using System.Reflection;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Eventing.InMemory;

/// <summary>
/// In-memory event bus implementation used for single-process deployments.
/// It resolves handlers from DI and optionally uses an inbox store for idempotency.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

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
        _logger.LogDebug("Publishing integration event {EventType} ({EventId})", eventType.FullName, @event.Id);

        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var handlers = ResolveHandlers(provider, eventType);
        if (handlers.Length == 0)
        {
            _logger.LogDebug("No handlers registered for integration event type {EventType}", eventType.FullName);
            return;
        }

        var inbox = provider.GetService<IInboxStore>();
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

        foreach (var handler in handlers)
        {
            await InvokeHandlerAsync(handler, handlerInterfaceType, eventType, @event, inbox, ct);
        }
    }

    private static object[] ResolveHandlers(IServiceProvider provider, Type eventType)
    {
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        return provider.GetServices(handlerInterfaceType).Where(h => h is not null).ToArray()!;
    }

    private async Task InvokeHandlerAsync(
        object handler,
        Type handlerInterfaceType,
        Type eventType,
        IIntegrationEvent @event,
        IInboxStore? inbox,
        CancellationToken ct)
    {
        var handlerName = handler.GetType().FullName ?? handler.GetType().Name;

        if (await ShouldSkipProcessedEventAsync(inbox, @event.Id, handlerName, ct))
        {
            _logger.LogDebug("Skipping already processed integration event {EventId} for handler {Handler}", @event.Id, handlerName);
            return;
        }

        var method = handlerInterfaceType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync));
        if (method == null)
        {
            _logger.LogWarning("Handler {Handler} does not implement HandleAsync correctly for {EventType}", handlerName, eventType.FullName);
            return;
        }

        await ExecuteHandlerAsync(handler, method, @event, eventType, handlerName, inbox, ct);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling integration event {EventId} with handler {Handler}", @event.Id, handlerName);
            throw;
        }
    }
}
