using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces.EventHandlers;

public class ProvinceUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<Province>>
{
    private readonly ILogger<ProvinceUpdatedEventHandler> _logger;

    public ProvinceUpdatedEventHandler(ILogger<ProvinceUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<Province> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}