using FSH.WebApi.Domain.Catalog.ChargeAggregate.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog.ChargeAggregate;

public class Charge
{
    public Charge(Guid id, Guid userId, int meterValue)
    {
        Id = id;
        UserId = userId;
        MeterValue = meterValue;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int MeterValue { get; private set; }
    public ChargeStatus Status { get; private set; } = ChargeStatus.Initiated;

    public ChargeInitiatedEvent InitiateCharge()
    {
        return new ChargeInitiatedEvent(Id, UserId);
    }

    public ChargeStartedEvent StartCharge()
    {
        return new ChargeStartedEvent(Id);
    }

    public ChargeReceivedMeterValueEvent ReceiveMeterValue(int meterValue)
    {
        return new ChargeReceivedMeterValueEvent(Id, meterValue);
    }

    public ChargeStoppedEvent StopCharge()
    {
        return new ChargeStoppedEvent(Id);
    }
}