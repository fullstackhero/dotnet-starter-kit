using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts.EventHandlers;

public class DistrictDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<District>>
{
    private readonly ILogger<DistrictDeletedEventHandler> _logger;

    public DistrictDeletedEventHandler(ILogger<DistrictDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<District> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}