using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards.EventHandlers;

public class WardDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<Ward>>
{
    private readonly ILogger<WardDeletedEventHandler> _logger;

    public WardDeletedEventHandler(ILogger<WardDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<Ward> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}