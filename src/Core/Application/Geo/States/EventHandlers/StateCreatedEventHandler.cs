using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States.EventHandlers;

public class StateCreatedEventHandler : EventNotificationHandler<EntityCreatedEvent<State>>
{
    private readonly ILogger<StateCreatedEventHandler> _logger;

    public StateCreatedEventHandler(ILogger<StateCreatedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityCreatedEvent<State> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}