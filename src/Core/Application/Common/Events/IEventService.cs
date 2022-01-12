namespace FSH.WebAPI.Application.Common.Events;

public interface IEventService : ITransientService
{
    Task PublishAsync(DomainEvent domainEvent);
}