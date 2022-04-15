using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameFilters.EventHandlers;

public class GameFilterCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<GameFilter>>
{
    private readonly ILogger<GameFilterCreatedEventHandler> _logger;

    public GameFilterCreatedEventHandler(ILogger<GameFilterCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<GameFilter> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}