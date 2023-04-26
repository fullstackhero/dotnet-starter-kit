using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States.EventHandlers;

public class StateDeletedEventHandler : EventNotificationHandler<EntityDeletedEvent<State>>
{
    private readonly ILogger<StateDeletedEventHandler> _logger;

    public StateDeletedEventHandler(ILogger<StateDeletedEventHandler> logger) => _logger = logger;

    public override Task Handle(EntityDeletedEvent<State> @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", @event.GetType().Name);
        return Task.CompletedTask;
    }
}