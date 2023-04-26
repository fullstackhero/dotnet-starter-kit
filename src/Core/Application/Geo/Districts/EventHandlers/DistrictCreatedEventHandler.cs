using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts.EventHandlers;

public class DistrictCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<District>>
{
    private readonly ILogger<DistrictCreatedEventHandler> _logger;

    public DistrictCreatedEventHandler(ILogger<DistrictCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<District> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}