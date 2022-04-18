using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Filters.EventHandlers;

public class FilterDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<Filter>>
{
    private readonly ILogger<FilterDeletedEventHandler> _logger;

    public FilterDeletedEventHandler(ILogger<FilterDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<Filter> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}