using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Features.Get.v1;
using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Category.Domain.Events;
 
public record CategoryItemCreated(Guid Id, string Name, string Description) : DomainEvent;

public class CategoryItemCreatedEventHandler(
    ILogger<CategoryItemCreatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<CategoryItemCreated>
{
    public async Task Handle(CategoryItemCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling categoryItem item created domain event..");
        var cacheResponse = new GetCategoryItemResponse(notification.Id, notification.Name, notification.Description);
        await cache.SetAsync($"categoryItem:{notification.Id}", cacheResponse, cancellationToken: cancellationToken);
    }
}
