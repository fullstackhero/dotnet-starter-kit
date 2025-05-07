using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.EventHandlers;

public class AgencyCreatedEventHandler(ILogger<AgencyCreatedEventHandler> logger) : INotificationHandler<AgencyCreated>
{
    public async Task Handle(AgencyCreated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("handling agency created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling agency created domain event..");
    }
}
