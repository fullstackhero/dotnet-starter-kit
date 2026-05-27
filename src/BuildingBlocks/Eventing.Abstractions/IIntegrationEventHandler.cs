namespace FSH.Framework.Eventing.Abstractions;

/// <summary>
/// Handles a single integration event type.
/// </summary>
/// <typeparam name="TEvent">The integration event type.</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}