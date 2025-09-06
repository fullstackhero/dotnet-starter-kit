namespace FSH.Framework.Core.Messaging.Events;
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent appEvent, CancellationToken cancellationToken = default);
}