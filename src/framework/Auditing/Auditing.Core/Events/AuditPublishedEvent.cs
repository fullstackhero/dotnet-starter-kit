using FSH.Framework.Auditing.Core.Dtos;
using MediatR;

namespace FSH.Framework.Auditing.Core.Events;

public class AuditPublishedEvent : INotification
{
    public IReadOnlyCollection<AuditTrail> Trails { get; }

    public AuditPublishedEvent(IReadOnlyCollection<AuditTrail> trails)
    {
        Trails = trails;
    }
}
