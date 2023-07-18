using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog.ChargeAggregate.Events;

public class ChargeReceivedMeterValueEvent : DomainEvent
{
    public ChargeReceivedMeterValueEvent(Guid chargeId, int meterValue)
    {
        ChargeId = chargeId;
        MeterValue = meterValue;
    }

    public Guid ChargeId { get; }
    public int MeterValue { get; }
}