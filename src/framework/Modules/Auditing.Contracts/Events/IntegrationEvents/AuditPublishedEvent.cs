using FSH.Framework.Auditing.Contracts.Dtos;
using FSH.Framework.Core.Messaging.Events;

namespace FSH.Framework.Auditing.Contracts.Events.IntegrationEvents;

public class AuditPublishedEvent : INotification
{
    public IReadOnlyCollection<TrailDto> Trails { get; }

    public AuditPublishedEvent(IReadOnlyCollection<TrailDto> trails)
    {
        Trails = trails;
    }
}
