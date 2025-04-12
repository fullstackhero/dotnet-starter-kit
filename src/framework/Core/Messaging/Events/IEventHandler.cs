namespace FSH.Framework.Core.Messaging.Events;
public interface IEventHandler<in TEvent>
{
    Task HandleAsync(TEvent appEvent, CancellationToken cancellationToken = default);
}
