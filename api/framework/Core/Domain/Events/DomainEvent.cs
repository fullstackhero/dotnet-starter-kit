namespace FSH.Framework.Core.Domain.Events;
public abstract class DomainEvent : IEvent
{
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}
