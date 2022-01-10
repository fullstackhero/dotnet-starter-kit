using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Application.Common.Events;

public interface IEventService : ITransientService
{
    Task PublishAsync(DomainEvent domainEvent);
}