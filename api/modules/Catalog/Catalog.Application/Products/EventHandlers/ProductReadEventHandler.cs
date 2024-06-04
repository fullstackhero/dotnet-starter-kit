using FSH.WebApi.Catalog.Domain.Events;
using Microsoft.Extensions.Logging;
using MediatR;

namespace FSH.WebApi.Catalog.Application.Products.EventHandlers;

public class ProductReadEventHandler(ILogger<ProductReadEventHandler> logger) : INotificationHandler<ProductRead>
{
    public async Task Handle(ProductRead notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("handling product read domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling product read domain event..");
    }
}

