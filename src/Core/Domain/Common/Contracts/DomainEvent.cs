using FL_CRMS_ERP_WEBAPI.Shared.Events;

namespace FL_CRMS_ERP_WEBAPI.Domain.Common.Contracts;

public abstract class DomainEvent : IEvent
{
    public DateTime TriggeredOn { get; protected set; } = DateTime.UtcNow;
}