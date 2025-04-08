using MediatR;

namespace FSH.Framework.Core.Domain.Events;
public abstract record DomainEvent : IDomainEvent, INotification
{
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}
