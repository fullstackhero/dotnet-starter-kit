using FSH.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Catalog.Application.Products.Creation.v1;

public class ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger) : INotificationHandler<ProductCreated>
{
    public async Task Handle(ProductCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling product created domain event..");
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("finished handling product created domain event..");
    }
}

