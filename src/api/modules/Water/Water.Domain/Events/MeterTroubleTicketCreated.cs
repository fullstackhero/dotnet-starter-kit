using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record MeterTroubleTicketCreated : DomainEvent
{
    public MeterTroubleTicket? MeterTroubleTicket { get; set; }
}
