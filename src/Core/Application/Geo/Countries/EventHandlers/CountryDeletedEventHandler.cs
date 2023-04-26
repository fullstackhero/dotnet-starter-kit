using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries.EventHandlers;

public class CountryDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<Country>>
{
    private readonly ILogger<CountryDeletedEventHandler> _logger;

    public CountryDeletedEventHandler(ILogger<CountryDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<Country> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}