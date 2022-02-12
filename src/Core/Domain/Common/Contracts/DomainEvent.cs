using FSH.WebApi.Shared.Events;

namespace FSH.WebApi.Domain.Common.Contracts;

public abstract class DomainEvent : IEvent
{
    public DateTime TriggeredOn { get; protected set; } = DateTime.UtcNow;
}