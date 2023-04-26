using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries.EventHandlers;

public class CountryUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<Country>>
{
    private readonly ILogger<CountryUpdatedEventHandler> _logger;

    public CountryUpdatedEventHandler(ILogger<CountryUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<Country> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}