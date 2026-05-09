using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Customers.EventHandlers;

public class CustomerUpdatedEventHandler(ILogger<CustomerUpdatedEventHandler> logger) : INotificationHandler<CustomerUpdated>
{
    public async Task Handle(CustomerUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling customer updated domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling customer updated domain event..");
    }
}
