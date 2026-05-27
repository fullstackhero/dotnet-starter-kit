namespace FSH.Framework.Eventing.Abstractions;

/// <summary>
/// Abstraction over an event bus. The initial provider is in-memory; additional providers
/// can be added without changing modules that publish or handle events.
/// </summary>
public interface IEventBus
{
    Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default);

    Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default);
}