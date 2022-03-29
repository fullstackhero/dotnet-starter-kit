using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameTypes.EventHandlers;

public class GameTypeUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<GameType>>
{
    private readonly ILogger<GameTypeUpdatedEventHandler> _logger;

    public GameTypeUpdatedEventHandler(ILogger<GameTypeUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<GameType> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}