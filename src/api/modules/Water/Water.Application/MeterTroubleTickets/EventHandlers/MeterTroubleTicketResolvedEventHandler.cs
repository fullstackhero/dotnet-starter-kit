using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.EventHandlers;

public class MeterTroubleTicketResolvedEventHandler(ILogger<MeterTroubleTicketResolvedEventHandler> logger) : INotificationHandler<MeterTroubleTicketResolved>
{
    public async Task Handle(MeterTroubleTicketResolved notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling meter trouble ticket resolved domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling meter trouble ticket resolved domain event..");
    }
}
