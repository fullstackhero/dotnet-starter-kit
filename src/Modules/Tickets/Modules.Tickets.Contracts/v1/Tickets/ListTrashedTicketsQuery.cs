using FSH.Framework.Shared.Persistence;
using FSH.Modules.Tickets.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record ListTrashedTicketsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<TicketDto>>;
