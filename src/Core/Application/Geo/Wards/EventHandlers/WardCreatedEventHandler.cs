using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards.EventHandlers;

public class WardCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<Ward>>
{
    private readonly ILogger<WardCreatedEventHandler> _logger;

    public WardCreatedEventHandler(ILogger<WardCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<Ward> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}