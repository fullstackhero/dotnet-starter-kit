using System.Collections.ObjectModel;
using FSH.Framework.Auditing.Models;
using MediatR;

namespace FSH.Framework.Auditing.Events;
public class AuditPublishedEvent : INotification
{
    public AuditPublishedEvent(Collection<AuditTrail>? trails)
    {
        Trails = trails;
    }
    public Collection<AuditTrail>? Trails { get; }
}
