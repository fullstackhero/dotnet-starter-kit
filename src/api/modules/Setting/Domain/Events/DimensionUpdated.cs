using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Domain.Events;
using FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Domain.Events;
public record DimensionUpdated(Dimension Item) : DomainEvent;

public class DimensionUpdatedEventHandler(
    ILogger<DimensionUpdatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<DimensionUpdated>
{
    public async Task Handle(DimensionUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling dimension item update domain event..");

        var cacheResponse = new GetDimensionResponse(
             notification.Item.Id,
             notification.Item.Order,
             notification.Item.Code,
             notification.Item.Name,
             notification.Item.Description,
             notification.Item.IsActive,
             notification.Item.FullName,
             notification.Item.NativeName,
             notification.Item.FullNativeName,
             notification.Item.Value,
             notification.Item.Type,
             notification.Item.FatherId
            );

        await cache.SetAsync($"dimension:{notification.Item.Id}", cacheResponse, cancellationToken: cancellationToken);
    }
}
