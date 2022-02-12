using FSH.WebApi.Shared.Events;

namespace FSH.WebApi.Application.Common.Events;

public interface IEventPublisher : ITransientService
{
    Task PublishAsync(IEvent @event);
}