using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.EntityCode.Domain.Events;
public record EntityCodeUpdated(global::FSH.Starter.WebApi.Setting.EntityCode.Domain.EntityCode Item) : DomainEvent;

public class DimensionUpdatedEventHandler(
    ILogger<DimensionUpdatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<EntityCodeUpdated>
{
    public async Task Handle(EntityCodeUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling dimension item update domain event..");

        var cacheResponse = new GetEntityCodeResponse(
             notification.Item.Id,
             notification.Item.Order,
             notification.Item.Code,
             notification.Item.Name,
             notification.Item.Description,
             notification.Item.IsActive,
             notification.Item.Separator,
             notification.Item.Value,
             notification.Item.Type
            );

        await cache.SetAsync($"entityCode:{notification.Item.Id}", cacheResponse, cancellationToken: cancellationToken);
    }
}
