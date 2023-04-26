using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts.EventHandlers;

public class DistrictUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<District>>
{
    private readonly ILogger<DistrictUpdatedEventHandler> _logger;

    public DistrictUpdatedEventHandler(ILogger<DistrictUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<District> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}