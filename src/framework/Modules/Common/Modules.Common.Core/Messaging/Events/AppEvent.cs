namespace FSH.Framework.Core.Messaging.Events;
public abstract record AppEvent : IEvent
{
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}