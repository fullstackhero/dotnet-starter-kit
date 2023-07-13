using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog.ChargeAggregate.Events;

public class ChargeInitiatedEvent : DomainEvent
{
    public ChargeInitiatedEvent(Guid chargeId, Guid userId)
    {
        ChargeId = chargeId;
        UserId = userId;
    }

    public Guid ChargeId { get; }
    public Guid UserId { get; }
}