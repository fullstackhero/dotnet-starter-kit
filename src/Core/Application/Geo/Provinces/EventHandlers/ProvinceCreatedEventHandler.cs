using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces.EventHandlers;

public class ProvinceCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<Province>>
{
    private readonly ILogger<ProvinceCreatedEventHandler> _logger;

    public ProvinceCreatedEventHandler(ILogger<ProvinceCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<Province> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}