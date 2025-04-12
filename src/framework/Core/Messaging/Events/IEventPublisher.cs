namespace FSH.Framework.Core.Messaging.Events;
public interface IEventPublisher<in TEvent> where TEvent : IEvent
{
    Task PublishAsync(TEvent appEvent, CancellationToken cancellationToken = default);
}
