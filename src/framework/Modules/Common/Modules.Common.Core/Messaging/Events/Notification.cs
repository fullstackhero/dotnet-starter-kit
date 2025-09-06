namespace FSH.Framework.Core.Messaging.Events;
public abstract record Notification : INotification
{
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}