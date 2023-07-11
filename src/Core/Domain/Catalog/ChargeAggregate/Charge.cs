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

    public void Apply(ChargeInitiatedEvent @event)
    {
        Id = @event.ChargeId;
        UserId = @event.UserId;
        MeterValue = @event.MeterValue;
        Status = ChargeStatus.Initiated;
    }

    public void Apply(ChargeStartedEvent @event)
    {
        Status = ChargeStatus.Active;
    }

    public void Apply(ChargeReceivedMeterValueEvent @event)
    {
        MeterValue = @event.MeterValue;
    }

    public void Apply(ChargeStoppedEvent @event)
    {
        Status = ChargeStatus.Stopped;
    }
}