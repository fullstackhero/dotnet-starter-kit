using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IEventService : ITransientService
{
    Task PublishAsync(DomainEvent domainEvent);
}