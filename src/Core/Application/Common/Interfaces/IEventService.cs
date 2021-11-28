using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IEventService : ITransientService
{
    Task PublishAsync(DomainEvent domainEvent);
}