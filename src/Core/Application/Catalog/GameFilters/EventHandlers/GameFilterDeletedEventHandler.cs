using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.GameFilters.EventHandlers;

public class GameFilterDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<GameFilter>>
{
    private readonly ILogger<GameFilterDeletedEventHandler> _logger;

    public GameFilterDeletedEventHandler(ILogger<GameFilterDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<GameFilter> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}