using FSH.Framework.Core.Domain;
using FSH.Modules.Tickets.Contracts.Dtos;

namespace FSH.Modules.Tickets.Domain.Events;

public sealed record TicketCreatedDomainEvent(
    Guid TicketId,
    string Number,
    string Title,
    TicketPriority Priority,
    Guid ReporterUserId,
    Guid? AssignedToUserId,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);
