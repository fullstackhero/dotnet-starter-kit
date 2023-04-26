using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries.EventHandlers;

public class CountryCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<Country>>
{
    private readonly ILogger<CountryCreatedEventHandler> _logger;

    public CountryCreatedEventHandler(ILogger<CountryCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<Country> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}