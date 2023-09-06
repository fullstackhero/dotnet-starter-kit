using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Filters.EventHandlers;

public class FilterCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<Filter>>
{
    private readonly ILogger<FilterCreatedEventHandler> _logger;

    public FilterCreatedEventHandler(ILogger<FilterCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<Filter> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}