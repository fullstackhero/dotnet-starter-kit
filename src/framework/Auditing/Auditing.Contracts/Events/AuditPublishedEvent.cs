using FSH.Framework.Core.Messaging.Events;

namespace FSH.Framework.Auditing.Contracts.Events;

public class AuditPublishedEvent : INotification
{
    public IReadOnlyCollection<Trail> Trails { get; }

    public AuditPublishedEvent(IReadOnlyCollection<Trail> trails)
    {
        Trails = trails;
    }
}
