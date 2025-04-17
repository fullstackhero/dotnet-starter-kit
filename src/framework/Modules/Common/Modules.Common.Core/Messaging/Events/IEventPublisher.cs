namespace FSH.Framework.Core.Messaging.Events;
public interface IEventPublisher
{
    Task PublishAsync(IEvent appEvent, CancellationToken cancellationToken = default);
}