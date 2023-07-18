using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog.ChargeAggregate.Events;

public class ChargeStartedEvent : DomainEvent
{
    public ChargeStartedEvent(Guid chargeId)
    {
        ChargeId = chargeId;
    }

    public Guid ChargeId { get; }
}