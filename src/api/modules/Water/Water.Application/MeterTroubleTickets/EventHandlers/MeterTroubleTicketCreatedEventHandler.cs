using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.EventHandlers;

public class MeterTroubleTicketCreatedEventHandler(ILogger<MeterTroubleTicketCreatedEventHandler> logger) : INotificationHandler<MeterTroubleTicketCreated>
{
    public async Task Handle(MeterTroubleTicketCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling meter trouble ticket created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling meter trouble ticket created domain event..");
    }
}
