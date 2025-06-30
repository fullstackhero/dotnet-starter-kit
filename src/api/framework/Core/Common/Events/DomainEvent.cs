using System;
using MediatR;

namespace FSH.Framework.Core.Common.Events;

public abstract class DomainEvent : INotification
{
    public DateTime OccurredOn { get; }
    public Guid Id { get; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}