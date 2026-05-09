using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record MeterTroubleTicketResolved : DomainEvent
{
    public MeterTroubleTicket? MeterTroubleTicket { get; set; }
}
