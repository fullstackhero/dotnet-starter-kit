using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameTypes.EventHandlers;

public class GameTypeCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<GameType>>
{
    private readonly ILogger<GameTypeCreatedEventHandler> _logger;

    public GameTypeCreatedEventHandler(ILogger<GameTypeCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<GameType> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}