using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Filters.EventHandlers;

public class FilterUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<Filter>>
{
    private readonly ILogger<FilterUpdatedEventHandler> _logger;

    public FilterUpdatedEventHandler(ILogger<FilterUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<Filter> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}