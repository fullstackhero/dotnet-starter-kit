using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States.EventHandlers;

public class StateUpdatedEventHandler : EventNotificationHandler<EntityUpdatedEvent<State>>
{
    private readonly ILogger<StateUpdatedEventHandler> _logger;

    public StateUpdatedEventHandler(ILogger<StateUpdatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityUpdatedEvent<State> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}