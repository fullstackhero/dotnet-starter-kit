using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Bills.EventHandlers;

public class BillCreatedEventHandler(ILogger<BillCreatedEventHandler> logger) : INotificationHandler<BillCreated>
{
    public async Task Handle(BillCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling bill created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling bill created domain event..");
    }
}
