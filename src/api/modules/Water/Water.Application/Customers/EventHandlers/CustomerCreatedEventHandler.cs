using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Customers.EventHandlers;

public class CustomerCreatedEventHandler(ILogger<CustomerCreatedEventHandler> logger) : INotificationHandler<CustomerCreated>
{
    public async Task Handle(CustomerCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling customer created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling customer created domain event..");
    }
}
