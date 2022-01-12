namespace DN.WebApi.Application.Common.Events;

public interface IEventService : ITransientService
{
    Task PublishAsync(DomainEvent domainEvent);
}