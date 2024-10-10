using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Domain.Events;
using FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Domain.Events;
public record EntityCodeUpdated(EntityCode Item) : DomainEvent;

public class EntityCodeUpdatedEventHandler(
    ILogger<EntityCodeUpdatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<EntityCodeUpdated>
{
    public async Task Handle(EntityCodeUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling EntityCode item update domain event..");

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
