using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record PaymentCreated : DomainEvent
{
    public Payment? Payment { get; set; }
}
