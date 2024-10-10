using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Domain.Events;
using FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Domain.Events;
public record EntityCodeCreated(
    Guid Id,
    int? Order,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string? Separation,
    int? Value,
    CodeType Type
    ) : DomainEvent;

public class EntityCodeCreatedEventHandler(
    ILogger<EntityCodeCreatedEventHandler> logger,
    ICacheService cache)
    : INotificationHandler<EntityCodeCreated>
{
    public async Task Handle(EntityCodeCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling EntityCode created domain event..");
        var cacheResponse = new GetEntityCodeResponse(
            notification.Id,
            notification.Order,
            notification.Code,
            notification.Name,
            notification.Description,
            notification.IsActive,
            notification.Separation,
            notification.Value,
            notification.Type
            );
        await cache.SetAsync($"entityCode:{notification.Id}", cacheResponse, cancellationToken: cancellationToken);
    }
}
