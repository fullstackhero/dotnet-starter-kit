using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameFilters.EventHandlers;

public class GameFilterUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<GameFilter>>
{
    private readonly ILogger<GameFilterUpdatedEventHandler> _logger;

    public GameFilterUpdatedEventHandler(ILogger<GameFilterUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<GameFilter> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}