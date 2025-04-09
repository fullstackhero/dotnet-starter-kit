namespace FSH.Framework.Core.Messaging.Events;
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}
