using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Update.v1;

public sealed class UpdateMeterTroubleTicketHandler(
    ILogger<UpdateMeterTroubleTicketHandler> logger,
    [FromKeyedServices("water:trouble-tickets")] IRepository<MeterTroubleTicket> repository)
    : IRequestHandler<UpdateMeterTroubleTicketCommand, UpdateMeterTroubleTicketResponse>
{
    public async Task<UpdateMeterTroubleTicketResponse> Handle(UpdateMeterTroubleTicketCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ticket = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = ticket ?? throw new MeterTroubleTicketNotFoundException(request.Id);
        var updatedTicket = ticket.Update(request.IssueDescription, request.Status, request.ResolutionNotes, request.ResolvedDate);
        await repository.UpdateAsync(updatedTicket, cancellationToken);
        logger.LogInformation("meter trouble ticket with id : {TicketId} updated.", ticket.Id);
        return new UpdateMeterTroubleTicketResponse(ticket.Id);
    }
}
