using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces.EventHandlers;

public class ProvinceDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<Province>>
{
    private readonly ILogger<ProvinceDeletedEventHandler> _logger;

    public ProvinceDeletedEventHandler(ILogger<ProvinceDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<Province> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}