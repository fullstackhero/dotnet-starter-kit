namespace FSH.Framework.Core.Messaging.Events;
public interface IEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
