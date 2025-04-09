namespace FSH.Framework.Core.Messaging.Events;
public abstract record DomainEvent : IDomainEvent
{
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}
