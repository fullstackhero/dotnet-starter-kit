using FL_CRMS_ERP_WEBAPI.Shared.Events;

namespace FL_CRMS_ERP_WEBAPI.Application.Common.Events;

public interface IEventPublisher : ITransientService
{
    Task PublishAsync(IEvent @event);
}