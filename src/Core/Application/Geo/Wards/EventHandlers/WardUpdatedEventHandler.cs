using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards.EventHandlers;

public class WardUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<Ward>>
{
    private readonly ILogger<WardUpdatedEventHandler> _logger;

    public WardUpdatedEventHandler(ILogger<WardUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<Ward> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}