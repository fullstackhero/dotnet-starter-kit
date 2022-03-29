using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameTypes.EventHandlers;

public class GameTypeDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<GameType>>
{
    private readonly ILogger<GameTypeDeletedEventHandler> _logger;

    public GameTypeDeletedEventHandler(ILogger<GameTypeDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<GameType> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}