using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Domain.Events;
using FSH.Starter.WebApi.Setting.Dimension.Features.v1;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Dimension.Domain.Events;
public record DimensionCreated(
    Guid Id,
    int? Order,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? FullName,
    string? NativeName,
    string? FullNativeName,
    int? Value,
    string Type,
    Guid? FatherId
    ) : DomainEvent;

public class DimensionCreatedEventHandler(
    ILogger<DimensionCreatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<DimensionCreated>
{
    public async Task Handle(DimensionCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling dimension created domain event..");
        var cacheResponse = new GetDimensionResponse(
            notification.Id,
            notification.Order,
            notification.Code,
            notification.Name,
            notification.Description,
            notification.IsActive,
            notification.FullName,
            notification.NativeName,
            notification.FullNativeName,
            notification.Value,
            notification.Type,
            notification.FatherId
            );
        await cache.SetAsync($"dimension:{notification.Id}", cacheResponse, cancellationToken: cancellationToken);
    }
}
