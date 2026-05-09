using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Delete.v1;

public sealed class DeleteMeterTroubleTicketHandler(
    ILogger<DeleteMeterTroubleTicketHandler> logger,
    [FromKeyedServices("water:trouble-tickets")] IRepository<MeterTroubleTicket> repository)
    : IRequestHandler<DeleteMeterTroubleTicketCommand>
{
    public async Task Handle(DeleteMeterTroubleTicketCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ticket = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = ticket ?? throw new MeterTroubleTicketNotFoundException(request.Id);
        await repository.DeleteAsync(ticket, cancellationToken);
        logger.LogInformation("meter trouble ticket with id : {TicketId} deleted", ticket.Id);
    }
}
