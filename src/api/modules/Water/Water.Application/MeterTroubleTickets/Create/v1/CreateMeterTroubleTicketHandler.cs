using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Create.v1;

public sealed class CreateMeterTroubleTicketHandler(
    ILogger<CreateMeterTroubleTicketHandler> logger,
    [FromKeyedServices("water:trouble-tickets")] IRepository<MeterTroubleTicket> repository)
    : IRequestHandler<CreateMeterTroubleTicketCommand, CreateMeterTroubleTicketResponse>
{
    public async Task<CreateMeterTroubleTicketResponse> Handle(CreateMeterTroubleTicketCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ticket = MeterTroubleTicket.Create(request.MeterId, request.ReportedDate, request.IssueDescription);
        await repository.AddAsync(ticket, cancellationToken);
        logger.LogInformation("meter trouble ticket created {TicketId}", ticket.Id);
        return new CreateMeterTroubleTicketResponse(ticket.Id);
    }
}
