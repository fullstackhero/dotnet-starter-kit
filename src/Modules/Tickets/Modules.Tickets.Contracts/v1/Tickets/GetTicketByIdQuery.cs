using FSH.Modules.Tickets.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record GetTicketByIdQuery(Guid TicketId) : IQuery<TicketDto>;
