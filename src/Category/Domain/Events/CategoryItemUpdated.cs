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
 
public record CategoryItemUpdated(CategoryItem item) : DomainEvent;

public class CategoryItemUpdatedEventHandler(
    ILogger<CategoryItemUpdatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<CategoryItemUpdated>
{
    public async Task Handle(CategoryItemUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling CategoryItem update domain event..");
        var cacheResponse = new GetCategoryItemResponse(notification.item.Id, notification.item.Name, notification.item.Description );
        await cache.SetAsync($"categoryItem:{notification.item.Id}", cacheResponse, cancellationToken: cancellationToken);
    }
}
